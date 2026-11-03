#!/usr/bin/env python3
"""Verify emitted recovery evidence bytes/completeness; never replace physics gates."""
import argparse
import csv
import hashlib
import json
from collections import Counter
from pathlib import Path


def require(condition, message):
    if not condition:
        raise ValueError(message)


def verify(root, production_mode, prior_summary=None, prior_mode=None):
    proof = root / 'h2-authority-proof'
    rows = json.loads((proof / 'summary.json').read_text(encoding='utf-8-sig'))
    modes = {'B0', 'H1', 'H2', 'H1H2', 'H2R', 'H2R3', 'H2R4', 'H2RF', 'H2RF3'}
    ids = {r['Id'] for r in rows}
    require(len(ids) == 174, 'Expected the declared 174-fixture matrix')
    require(len(rows) == len(ids) * len(modes), 'Incomplete mode/fixture matrix')
    require(len({(r['Id'], r['Mode']) for r in rows}) == len(rows), 'Duplicate mode/fixture row')
    for fixture in ids:
        require({r['Mode'] for r in rows if r['Id'] == fixture} == modes, f'{fixture}: incomplete modes')
    selected = [r for r in rows if r['Mode'] == production_mode]
    require(len(selected) == 174, 'Missing production reference mode')
    for r in selected:
        require(r['CandidateSHA256'] == r['ProductionCandidateSHA256'] and
                r['AuthoritySHA256'] == r['ProductionAuthoritySHA256'], f"{r['Id']}: public/mode disagreement")
        if r['Status'] in ('Accepted', 'BudgetExhausted'):
            require(r['ObservedProductionSteps'] == r['FeedbackIterations'], f"{r['Id']}: incomplete observer")
        if r['Status'] == 'Accepted':
            require(r['FixedPointRawFieldCount'] > 0, f"{r['Id']}: missing raw fixed-point observation")
            require(r['Metrics']['EndpointRawBitsEqual'] and r['Metrics']['TraceBoundaryForceRawBitsEqual'],
                    f"{r['Id']}: missing existing endpoint/force identity evidence")
    prior_count = None
    if prior_summary:
        prior = {r['Id']: r for r in json.loads(prior_summary.read_text(encoding='utf-8-sig')) if r['Mode'] == prior_mode}
        require(set(prior) == ids, 'Prior/current fixture sets differ')
        prior_count = 0
        for r in selected:
            if prior[r['Id']]['Status'] == 'Accepted':
                require(all(r[k] == prior[r['Id']][k] for k in ('Status', 'CandidateSHA256', 'AuthoritySHA256')),
                        f"{r['Id']}: prior Accepted changed")
                prior_count += 1
    fixed_files = list(proof.glob('*-fixed-point.csv'))
    require(len(fixed_files) == 50, 'Missing declared core/residual fixed-point transcripts')
    fixed_pairs = 0
    for path in fixed_files:
        count = 0
        with path.open(encoding='utf-8-sig', newline='') as stream:
            for i, row in enumerate(csv.DictReader(stream)):
                require(int(row['FieldIndex']) == i, f'{path.name}: nonconsecutive field')
                require(row['PreviousRawBits'] == row['NextRawBits'], f'{path.name}: unequal fixed-point field')
                count += 1
        fixture, mode = path.name.removesuffix('-fixed-point.csv').rsplit('-', 1)
        summary = next(r for r in rows if r['Id'] == fixture and r['Mode'] == mode)
        require(count == summary['FixedPointRawFieldCount'] and count > 0, f'{path.name}: truncated transcript')
        fixed_pairs += count
    observed_known_steps = 0
    production_by_id = {r['Id']: r for r in selected}
    observed_files = list(proof.glob('*-' + production_mode + '.json'))
    require(len(observed_files) == 12, 'Missing full core/residual public observer evidence')
    for path in observed_files:
        evidence = json.loads(path.read_text(encoding='utf-8-sig'))
        summary = production_by_id[evidence['Id']]
        stages = evidence['ObservedProductionStages']
        require(len(stages) == summary['ObservedProductionSteps'], 'Observed stage count differs')
        known_count = 0
        for stage in stages:
            fields = {(f['Stage'], f['Segment'], f['Name']): f['Bits'] for f in stage['Fields']}
            require(len(fields) == len(stage['Fields']), 'Duplicate observed field identity')
            source = fields.get(('After', 0, 'KnownBuoyDragSourceN'))
            if source is not None:
                require(source == fields[('After', 0, 'BoundaryBuoyDragN')] == fields[('After', 0, 'TraceInitialHN')],
                        'Known-buoy source/boundary/trace raw identity differs')
                known_count += 1
        require(known_count == summary['ObservedKnownBuoySteps'], 'Known-buoy observer count differs')
        observed_known_steps += known_count
    require(observed_known_steps > 0, 'Known-buoy production recovery was not observed')
    survey = root / 'raw-cycle-survey'
    witnesses = json.loads((survey / 'raw-cycle-summary.json').read_text(encoding='utf-8-sig'))
    require(len(witnesses) == 15 and len({(r['Id'], r['Mode']) for r in witnesses}) == 15, 'Incomplete cycle survey')
    vector_fields = 0
    for r in witnesses:
        require(r['RawReplayIdentical'] and r['ObserverNoninterference'], 'Missing raw replay/noninterference evidence')
        states = {s['Iteration']: s for s in r['States']}
        require(set(states) == set(range(r['FeedbackIterations'] + 1)), 'Incomplete observed trajectory')
        with (survey / f"{r['Id']}-{r['Mode']}-terminal-period.csv").open(encoding='utf-8-sig', newline='') as stream:
            raw = list(csv.DictReader(stream))
        require(raw and all(int(row['FieldIndex']) == i for i, row in enumerate(raw)), 'Invalid cycle field sequence')
        keys = list(raw[0])[1:]
        period = r['TerminalRawPeriod']
        require(period in (1, 2, 3) and len(keys) == period + 1, 'Unexpected terminal witness schema')
        vectors = [[row[key] for row in raw] for key in keys]
        require(vectors[0] == vectors[-1] and len({tuple(v) for v in vectors[:-1]}) == period,
                'Cycle endpoint/intermediate raw vectors disagree')
        for key, vector in zip(keys, vectors):
            iteration = int(key.removeprefix('Iteration'))
            require(iteration >= r['LastMapStart'], 'Cycle witness crosses arithmetic maps')
            require(len(vector) == states[iteration]['FieldCount'], 'Truncated cycle vector')
            require(hashlib.sha256('\n'.join(vector).encode()).hexdigest().upper() == states[iteration]['SHA256'],
                    'Cycle witness digest differs')
        vector_fields += len(raw) * len(keys)
    return {'Rows': len(rows), 'Fixtures': len(ids), 'DistinctInputHashes': len({r['InputHash'] for r in selected}),
            'ProductionMode': production_mode, 'Statuses': dict(Counter(r['Status'] for r in selected)),
            'PriorAcceptedPreserved': prior_count, 'FixedPointTranscripts': len(fixed_files),
            'EqualFixedPointFields': fixed_pairs, 'CycleWitnesses': len(witnesses), 'CycleVectorFields': vector_fields,
            'ObservedKnownBuoyStepsVerified': observed_known_steps,
            'PhysicalValidation': 'NOT_ESTABLISHED_BY_BYTE_VERIFICATION'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('artifact_directory', type=Path)
    parser.add_argument('--production-mode', required=True)
    parser.add_argument('--prior-summary', type=Path)
    parser.add_argument('--prior-mode')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    result = verify(args.artifact_directory, args.production_mode, args.prior_summary, args.prior_mode)
    output = json.dumps(result, indent=2) + '\n'
    if args.output:
        args.output.write_text(output)
    print(output, end='')


if __name__ == '__main__':
    main()

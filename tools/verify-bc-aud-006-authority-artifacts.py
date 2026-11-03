#!/usr/bin/env python3
"""Verify emitted recovery evidence bytes/completeness; never replace physics gates."""
import argparse
import csv
import hashlib
import json
import sys
from collections import Counter
from pathlib import Path


def require(condition, message):
    if not condition:
        raise ValueError(message)


def verify(root, production_mode, prior_summary=None, prior_mode=None, perpendicular_research=False,
           fixture_count=174, accepted_baseline=None, require_candidate_baseline=False):
    proof = root / 'h2-authority-proof'
    rows = json.loads((proof / 'summary.json').read_text(encoding='utf-8-sig'))
    modes = {'B0', 'H1', 'H2', 'H1H2', 'H2R', 'H2R3', 'H2R4', 'H2RF', 'H2RF3'}
    if perpendicular_research:
        modes |= {'H2RF3P', 'H2RF3Q', 'H2RF3QK'}
    ids = {r['Id'] for r in rows}
    require(len(ids) == fixture_count, 'Fixture matrix does not match its declared count')
    require(len(rows) == len(ids) * len(modes), 'Incomplete mode/fixture matrix')
    require(len({(r['Id'], r['Mode']) for r in rows}) == len(rows), 'Duplicate mode/fixture row')
    for fixture in ids:
        require({r['Mode'] for r in rows if r['Id'] == fixture} == modes, f'{fixture}: incomplete modes')
    selected = [r for r in rows if r['Mode'] == production_mode]
    require(len(selected) == fixture_count, 'Missing production reference mode')
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
        require(set(prior) <= ids, 'A prior fixture was removed')
        prior_count = 0
        for r in selected:
            if r['Id'] in prior and prior[r['Id']]['Status'] == 'Accepted':
                require(all(r[k] == prior[r['Id']][k] for k in ('Status', 'CandidateSHA256', 'AuthoritySHA256')),
                        f"{r['Id']}: prior Accepted changed")
                prior_count += 1
    baseline_checked = 0
    candidate_baseline_checked = False
    if accepted_baseline:
        baseline = json.loads(accepted_baseline.read_text(encoding='utf-8-sig'))
        require(baseline['SourceCommit'] == '66e4327a3b93fe577725465f4a6368d0aedf3c01' and
                baseline['SourceArtifactSHA256'] == 'fa7fa500aca0dadcf19d52c0ca3bfef934e00deec1c88f5bf736acd5366ddbd1',
                'Unexpected frozen prior Accepted provenance')
        entries = baseline['Entries']
        require(len(entries) == 149 and len({r['Id'] for r in entries}) == 149, 'Frozen Accepted baseline incomplete')
        current = {r['Id']: r for r in selected}
        candidate_baseline_checked = require_candidate_baseline or sys.platform == 'win32'
        for old in entries:
            require(old['Id'] in current, 'Frozen Accepted fixture removed')
            r = current[old['Id']]
            require(r['Status'] == 'Accepted' and r['InputHash'] == old['InputHash'] and
                    r['AuthoritySHA256'] == old['AuthoritySHA256'], 'Frozen prior Accepted authority/input changed')
            if candidate_baseline_checked:
                require(r['CandidateSHA256'] == old['CandidateSHA256'], 'Frozen prior Windows candidate changed')
            baseline_checked += 1
    fixed_files = list(proof.glob('*-fixed-point.csv'))
    public_full_files = list(proof.glob('*-' + production_mode + '.json'))
    core_ids = {json.loads(path.read_text(encoding='utf-8-sig'))['Id'] for path in public_full_files}
    require(len(core_ids) == 12, 'Missing declared core/residual fixture evidence')
    expected_fixed = {(r['Id'], r['Mode']) for r in rows if r['Id'] in core_ids and r['FixedPointRawFieldCount'] > 0}
    actual_fixed = {tuple(path.name.removesuffix('-fixed-point.csv').rsplit('-', 1)) for path in fixed_files}
    require(actual_fixed == expected_fixed, 'Missing or unexpected fixed-point transcript')
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
    survey_modes = {'H2R3', 'H2RF', 'H2RF3'} | ({'H2RF3P', 'H2RF3Q', 'H2RF3QK'} if perpendicular_research else set())
    survey_ids = {r['Id'] for r in witnesses}
    require(len(survey_ids) == 5 and len(witnesses) == len(survey_modes) * 5 and
            {(r['Id'], r['Mode']) for r in witnesses} == {(i, m) for i in survey_ids for m in survey_modes},
            'Incomplete cycle survey')
    vector_fields = 0
    reset_fields = 0
    reset_count = 0
    for r in witnesses:
        require(r['RawReplayIdentical'] and r['ObserverNoninterference'], 'Missing raw replay/noninterference evidence')
        states = {s['Iteration']: s for s in r['States']}
        require(set(states) == set(range(r['FeedbackIterations'] + 1)), 'Incomplete observed trajectory')
        resets = r.get('CanonicalResets', [])
        for reset in resets:
            require(reset['SourceStateIteration'] == 0 and reset['RawBitsMatchState0'] and
                    reset['SourceSHA256'] == states[0]['SHA256'], 'Canonical reset source metadata differs')
            source_file = survey / f"{r['Id']}-{r['Mode']}-restart-source.csv"
            with source_file.open(encoding='utf-8-sig', newline='') as stream:
                source_rows = list(csv.DictReader(stream))
            require(len(source_rows) == states[0]['FieldCount'] == reset['FieldCount'], 'Truncated reset source')
            require(all(int(row['FieldIndex']) == i and row['InitialRawBits'] == row['ResetRawBits']
                        for i, row in enumerate(source_rows)), 'Canonical reset raw source changed')
            digest = hashlib.sha256('\n'.join(row['ResetRawBits'] for row in source_rows).encode()).hexdigest().upper()
            require(digest == reset['SourceSHA256'], 'Reset source digest differs')
            reset_fields += len(source_rows)
            reset_count += 1
        witness_file = survey / f"{r['Id']}-{r['Mode']}-terminal-period.csv"
        if r['TerminalRawPeriod'] == 0 and perpendicular_research and r['Mode'] in {'H2RF3P', 'H2RF3Q', 'H2RF3QK'}:
            require(r['Status'] != 'Accepted' and not witness_file.exists(), 'Nonrepeating trajectory falsely labelled Accepted')
            continue
        with witness_file.open(encoding='utf-8-sig', newline='') as stream:
            raw = list(csv.DictReader(stream))
        require(raw and all(int(row['FieldIndex']) == i for i, row in enumerate(raw)), 'Invalid cycle field sequence')
        keys = list(raw[0])[1:]
        period = r['TerminalRawPeriod']
        require(1 <= period <= 64 and (perpendicular_research or period in (1, 2, 3)) and len(keys) == period + 1,
                'Unexpected terminal witness schema')
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
    if perpendicular_research:
        require(reset_count > 0, 'Canonical reset branch was not exercised')
    return {'Rows': len(rows), 'Fixtures': len(ids), 'DistinctInputHashes': len({r['InputHash'] for r in selected}),
            'ProductionMode': production_mode, 'Statuses': dict(Counter(r['Status'] for r in selected)),
            'PriorAcceptedPreserved': prior_count, 'FixedPointTranscripts': len(fixed_files),
            'EqualFixedPointFields': fixed_pairs, 'CycleWitnesses': len(witnesses), 'CycleVectorFields': vector_fields,
            'ObservedKnownBuoyStepsVerified': observed_known_steps, 'CanonicalResetWitnesses': reset_count,
            'CanonicalResetRawFields': reset_fields, 'FrozenAcceptedAuthoritiesVerified': baseline_checked,
            'FrozenWindowsCandidatesChecked': candidate_baseline_checked,
            'PhysicalValidation': 'NOT_ESTABLISHED_BY_BYTE_VERIFICATION'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('artifact_directory', type=Path)
    parser.add_argument('--production-mode', required=True)
    parser.add_argument('--prior-summary', type=Path)
    parser.add_argument('--prior-mode')
    parser.add_argument('--perpendicular-research', action='store_true')
    parser.add_argument('--fixture-count', type=int, default=174)
    parser.add_argument('--accepted-baseline', type=Path)
    parser.add_argument('--require-candidate-baseline', action='store_true')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    result = verify(args.artifact_directory, args.production_mode, args.prior_summary, args.prior_mode, args.perpendicular_research,
                    args.fixture_count, args.accepted_baseline, args.require_candidate_baseline)
    output = json.dumps(result, indent=2) + '\n'
    if args.output:
        args.output.write_text(output)
    print(output, end='')


if __name__ == '__main__':
    main()

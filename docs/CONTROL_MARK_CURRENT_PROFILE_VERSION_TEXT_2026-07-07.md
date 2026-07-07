# CurrentProfileWindow version text control mark

Date: 2026-07-07
Related issue: #148

Current XAML contains an old version in the window title and in the top-right badge. Runtime code already replaces both with the current application version.

The production change may update only `Views/CurrentProfileWindow.axaml`:

- add the services namespace;
- use the version-free title `Профиль течения по глубине`;
- use `AppInfo.DisplayVersion` for the top-right badge through `x:Static`.

The constructor and `WindowVersionHelper.Apply` call remain unchanged. The introductory paragraph containing `В v0.19 ...` also remains unchanged because it is functional guidance, not a technical badge.

All profile bindings, commands, points, summaries, persistence, calculations, solver behavior, PDF, 2D, reports, JSON, DTOs, layout, and application version remain unchanged.

Visible output remains:

- title: `Профиль течения по глубине v0.46.4`;
- badge: `v0.46.4 - пользовательские статусы PDF`.

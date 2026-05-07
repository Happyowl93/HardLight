# Consent system

OOC consent preferences (toggles + freetext) ported from the HardLight fork
(originally Floofstation). Players manage their preferences in a top-bar window
or via the `OpenConsentWindow` keybind (default <kbd>J</kbd>); systems read the
toggles to decide whether to apply optional mechanics or render optional content
to that player.

## Architecture

| Layer | Type | Role |
|---|---|---|
| Server | `IServerConsentManager` / `ServerConsentManager` | Source of truth. Loads from DB on connect, saves on update, exposes lookup by `NetUserId`. |
| Client | `IClientConsentManager` / `ClientConsentManager` | Holds the local player's toggles; sends updates to the server. |
| Shared | `ConsentComponent` | Mirrors a player's toggles onto their attached entity (so prediction works and other systems can `HasConsent(entity, toggleId)`). |
| Shared | `SharedConsentSystem` | Public API (`HasConsent`) and the `Consent Info` examine verb. |
| Server | `ConsentSystem` | Attaches/syncs `ConsentComponent` onto player-controlled entities. |
| Database | `ConsentSettings`, `ConsentToggle`, `ConsentFreetextReadReceipt` | EF Core entities + `NovaConsentSystem` migration. |

Adding a new toggle is just a YAML entry in
[Resources/Prototypes/consent.yml](../../../Resources/Prototypes/consent.yml) — the menu
auto-generates the checkbox. Override the displayed name/description in
[Resources/Locale/en-US/Blep/consent.ftl](../../../Resources/Locale/en-US/Blep/consent.ftl)
with `consent-<ID>` / `consent-<ID>.desc`.

## Reading a toggle from your system

```csharp
[Dependency] private readonly SharedConsentSystem _consent = default!;

private static readonly ProtoId<ConsentTogglePrototype> MyToggle = "MyToggle";

if (_consent.HasConsent(targetEntity, MyToggle))
{
    // ...
}
```

Entities never controlled by a player return `true` for any toggle — the
convention is "no player has ever said no, so go ahead."

## Gating an entity effect on consent

`ConsentCondition` plugs into NovaSector's `EntityCondition` framework. Use it
inside any reagent or status effect:

```yaml
- !type:HealthChange
  conditions:
  - !type:ConsentCondition
    consent: Aphrodisiacs
  damage: ...
```

Defined in [Content.Shared/EntityConditions/Conditions/ConsentEntityConditionSystem.cs](../../EntityConditions/Conditions/ConsentEntityConditionSystem.cs).

## Current toggles and their consumers

| Toggle | Read by |
|---|---|
| `NSFWDescriptions` | `SharedCustomExamineSystem` — hides examine text marked NSFW. |
| `GenitalMarkings` | `HumanoidAppearanceSystem` — hides markings with `MarkingCategories.Genital`. |
| `NonconIcon` | `ShowNonconIconsSystem` — mutual opt-in HUD overlay. |
| `Aphrodisiacs` | Pomelustine / Philterex reagent metabolism (via `ConsentCondition`). |
| `MindControl` | `BrainwasherSystem` — verb is hidden and engagement aborts without consent. |
| `Cum` | `LewdTraitSystem` — hides "Cum inside" verb on non-consenting targets. |

## Database

Persisted in `ConsentSettings` (one row per `NetUserId`) with a
collection of `ConsentToggle` rows. The schema is created by the
`NovaConsentSystem` EF migration in
[Content.Server.Database/Migrations/](../../../Content.Server.Database/Migrations/).
Loading happens through `IServerConsentManager.LoadData`, which is registered
into [`UserDbDataManager`](../../../Content.Server/Database/UserDbDataManager.cs)
via `IPostInjectInit.PostInject` on the manager — the same hook
`IServerPreferencesManager` uses.

## Read receipts

`ConsentFreetextReadReceipt` rows track which players have read which target's
consent freetext, and when. The read indicator (planned: red dot on the verb
icon) currently has no asset attached, so the verb always renders without the
indicator. To enable: drop a `consent_examine_with_red_dot.svg.192dpi.png` at
`Resources/Textures/_Common/Interface/VerbIcons/` and the existing path in
`SharedConsentSystem.OnGetExamineVerbs` will pick it up automatically.

## See also

- `Content.Shared/_Floof/Examine/` — Custom Examine, the original NSFW consumer.
- `Content.Server/_HL/Brainwashing/` — neuralyzer system, gates on `MindControl`.
- `Content.Server/FloofStation/Traits/LewdTraitSystem.cs` — fluid producers,
  gates on `Cum`.
- HardLight upstream: <https://github.com/HardLightSector/HardLight>

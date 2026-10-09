# Website and product documentation maintenance

Apply this policy whenever a change affects user-visible job steps or their fields, options,
value sources, results, defaults, validation, execution or error behavior; automations and their
triggers, actions or run policies; macro commands, recording, editing or timing; or logs, statuses,
codes, presentation, retention or export.

- Identify affected product documentation before editing the canonical implementation.
- Update the corresponding explanations, field and option reference, examples and cross-links
  in the same task. Cover nested values and dynamic or conditional variants as applicable.
- Use existing catalogs, models, validators and runtime owners as the source of truth. Never
  introduce a second independent rule merely to describe a feature on the website.
- Update example packages when their behavior or configuration changes. Validate them with the
  canonical serializers and validators; distinguish configuration validation from execution.
- Refresh affected screenshots with real application rendering and anonymous fixture data.
  The website's standard screenshots, including logs, use the application Light theme.
  Use `../skills/maintain-website-screenshots/SKILL.md` and the canonical catalog
  `website/docs/screenshots.json`. Render affected IDs, inspect the images and their website
  placement, and record visual approval before completing the change. Keep source dependencies
  and documentation associations current; add a capture for a newly illustrated function.
  Use focused, annotated detail captures next to the explanation. Keep numbered field legends
  and schema visualizations consistent with the UI and JSON/agent references. Whole-window
  screenshots are gallery assets and do not replace field-level visual guidance in the Doku.
- Preserve documentation URLs and identify the supported app version. Do not present unreleased
  behavior as the behavior of the current downloadable release.
- Once a documentation exporter/generator and coverage check exist, regenerate affected output
  and run the relevant check through the existing verification entrypoint. Fix missing coverage,
  stale output, invalid examples and broken references before claiming completion.
- Automated metadata export does not replace an agent's review of behavior explanations.
  Behavior-only changes require that review even if the exported schema is unchanged.
- Report documentation updates and validation in the completion response. If no documentation
  update is needed, record a specific impact-based reason; do not silently omit the review.

The structure is described in [the website concept](../../website/CONCEPT.md). The implemented
export/generation workflow is documented in [the documentation maintenance guide](../../website/docs/README.md).
For applicable changes, update `website/docs/content.json`, `website/tools/guide_content.py` and
examples. Maintain both the UI workflow and exact JSON authoring instructions, including types,
units, provider references and compound schemas. The generated `llms.txt`, `llms-full.txt`,
`doku/referenz.md` and `doku/vertragsdaten.json` must stay consistent with the website.
Run `website/Build.ps1`,
and verify through `eng/verify.ps1 -Mode Focused -Checks website-docs,documentation,skills`.
The `website-docs` check is also part of Full verification. It rejects stale contracts, stale output,
invalid examples and missing reference keys or explanations. Full documentation is now required:
`website/docs/pending.json` has no entries and must not be expanded to bypass missing content.
The verification check runs `website/Build.ps1 -Check -Strict`; every explanation must be present.
Do not run a recurring background task or publish a website merely because code changed.

{
  "meta": {
    "project": "Agente IA Local",
    "prompt_kind": "refactor_onion",
    "prompt_id": "REFACTOR_ONION_V1"
  },
  "context": {
    "workspace_reference": "@workspace",
    "explicit_references": [
      "<ADD: #File.cs/#Folder>",
      "#README.architecture.es.md",
      "#Reglas.IA.md"
    ]
  },
  "objective": "<ONE_SENTENCE_REFACTOR_GOAL>",
  "constraints": {
    "must": [
      "Onion: abstractions/interfaces in Core or Application, implementations in Infrastructure/UI",
      "No new dependency from Core to Infrastructure/UI/VSIX",
      "Keep public surface behavior unchanged unless explicitly required"
    ],
    "must_not": [
      "Do not modify: *.vsix, *.vsct, *.vsixmanifest, *.csproj, *.sln"
    ]
  },
  "steps": [
    {
      "step": 1,
      "action": "Propose target file/class moves and dependency direction check (no code changes yet).",
      "requires_human_confirmation_before_applying": true
    },
    {
      "step": 2,
      "action": "Apply the refactor in small commits of changes (smallest set of files per iteration).",
      "requires_human_confirmation_before_next_step": true
    }
  ],
  "acceptance_criterion": "<SINGLE_VERIFIABLE_CRITERION (e.g., 'Core has zero references to Infrastructure namespaces after refactor')>",
  "response_format": {
    "json_only": true,
    "must_list_changes": true
  }
}

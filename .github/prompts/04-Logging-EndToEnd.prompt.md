{
  "meta": {
    "project": "Agente IA Local",
    "prompt_kind": "logging_end_to_end",
    "prompt_id": "LOGGING_E2E_V1"
  },
  "context": {
    "workspace_reference": "@workspace",
    "explicit_references": [
      "<ADD: #File.cs where execution starts (Run/Execute command)>",
      "<ADD: #LogEntryTextFormatter (if exists)>",
      "<ADD: #Logger implementation / Serilog setup>"
    ],
    "current_problem": "<ONE_SENTENCE_LOGGING_PROBLEM>"
  },
  "objective": "Make logging consistent end-to-end for a single execution.",
  "constraints": {
    "must": [
      "All output goes through the single formatter (no duplicate field rendering)",
      "CorrelationId generated once per execution and propagated end-to-end",
      "No behavior change besides logging"
    ],
    "must_not": [
      "Do not modify: *.vsix, *.vsct, *.vsixmanifest, *.csproj, *.sln"
    ]
  },
  "steps": [
    {
      "step": 1,
      "action": "Locate existing formatter and CorrelationId handling; propose minimal propagation path.",
      "requires_human_confirmation_before_applying": true
    },
    {
      "step": 2,
      "action": "Implement propagation and update log calls; add NEW markers on touched methods if required.",
      "requires_human_confirmation_before_next_step": true
    }
  ],
  "acceptance_criterion": "<SINGLE_VERIFIABLE_CRITERION (e.g., 'Every log line includes the same CorrelationId for one Run execution and fields are not duplicated')>",
  "response_format": {
    "json_only": true,
    "must_list_changes": true
  }
}

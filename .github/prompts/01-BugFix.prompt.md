{
  "meta": {
    "project": "Agente IA Local",
    "prompt_kind": "bugfix",
    "prompt_id": "BUGFIX_V1"
  },
  "context": {
    "workspace_reference": "@workspace",
    "explicit_references": [
      "<ADD: #output>",
      "<ADD: #File.cs/#File.xaml where bug occurs>",
      "<ADD: #MethodOrClassName>"
    ],
    "repro": {
      "steps": [
        "<STEP_1>",
        "<STEP_2>"
      ],
      "expected": "<EXPECTED_BEHAVIOR>",
      "actual": "<ACTUAL_BEHAVIOR>"
    }
  },
  "objective": "Fix the bug with the smallest safe change.",
  "constraints": {
    "must": [
      "Keep existing architecture boundaries (UI/Application/Core/Infrastructure)",
      "Add/adjust logging only if it helps diagnose or ensures traceability",
      "Update UI state properties with correct PropertyChanged notifications when applicable"
    ],
    "must_not": [
      "Do not modify: *.vsix, *.vsct, *.vsixmanifest, *.csproj, *.sln",
      "Do not introduce breaking API changes without explicit human request"
    ]
  },
  "steps": [
    {
      "step": 1,
      "action": "Identify root cause from referenced code and #output logs.",
      "requires_human_confirmation_before_applying": true
    },
    {
      "step": 2,
      "action": "Implement minimal fix and update/extend tests if a test project exists for that area.",
      "requires_human_confirmation_before_next_step": true
    }
  ],
  "acceptance_criterion": "<SINGLE_VERIFIABLE_CRITERION (e.g., 'State button updates to Stop within 200ms after Run is clicked')>",
  "response_format": {
    "json_only": true,
    "must_list_changes": true,
    "must_include_new_markers": true
  }
}

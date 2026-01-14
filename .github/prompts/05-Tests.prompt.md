{
  "meta": {
    "project": "Agente IA Local",
    "prompt_kind": "tests",
    "prompt_id": "TESTS_V1"
  },
  "context": {
    "workspace_reference": "@workspace",
    "explicit_references": [
      "<ADD: #ClassOrMethodUnderTest>",
      "<ADD: #ExistingTestProject (e.g., AgenteIALocal.Tests)>"
    ],
    "test_goal": "<ONE_SENTENCE_TEST_GOAL>"
  },
  "objective": "Add/adjust automated tests that cover test_goal.",
  "constraints": {
    "must": [
      "Tests must be deterministic (no timing flakiness)",
      "Use existing test framework and patterns in the repo",
      "Prefer unit tests over integration unless explicitly needed"
    ],
    "must_not": [
      "Do not change production behavior to satisfy tests unless required by bugfix",
      "Do not modify VSIX critical files"
    ]
  },
  "steps": [
    {
      "step": 1,
      "action": "Locate current test framework and propose test cases (no code changes yet).",
      "requires_human_confirmation_before_applying": true
    },
    {
      "step": 2,
      "action": "Implement tests and any minimal seams required for testability within architecture boundaries.",
      "requires_human_confirmation_before_next_step": true
    }
  ],
  "acceptance_criterion": "<SINGLE_VERIFIABLE_CRITERION (e.g., 'New tests fail before fix and pass after fix')>",
  "response_format": {
    "json_only": true,
    "must_list_changes": true
  }
}

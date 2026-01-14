{
  "meta": {
    "project": "Agente IA Local",
    "prompt_kind": "context_gathering",
    "prompt_id": "CONTEXT_GATHER_V1"
  },
  "context": {
    "workspace_reference": "@workspace",
    "explicit_references": [
      "#Reglas.IA.md",
      "<ADD: short description of what you need to do>"
    ]
  },
  "objective": "Identify the minimum set of files/methods/logs that must be referenced to solve the task safely.",
  "constraints": {
    "must": [
      "No code edits in this step",
      "List concrete filenames and symbols to reference via #...",
      "If build/runtime info is required, ask for #output logs"
    ],
    "must_not": [
      "Do not propose changes yet"
    ]
  },
  "steps": [
    {
      "step": 1,
      "action": "Scan @workspace conceptually and output a shortlist of references to attach.",
      "requires_human_confirmation_before_next_step": false
    }
  ],
  "acceptance_criterion": "Output contains an ordered list of <= 10 concrete #references that, when attached, would be sufficient to proceed.",
  "response_format": {
    "json_only": true,
    "required_keys": [
      "status",
      "references_to_attach",
      "why_each_reference"
    ]
  }
}

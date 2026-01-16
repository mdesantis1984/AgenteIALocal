---
name: 'task-template'
description: 'Plantilla de tarea (paso a paso, JSON tecnico)'
agent: 'agent'
argument-hint: 'objective="..." acceptance="..." refs="#File.cs #output ..."'
---

Ejecuta este prompt y responde SOLO con JSON valido (sin texto adicional).
Si falta contexto: pide #File, #output, @workspace y marca status="needs_confirmation".

Plantilla JSON (rellena placeholders entre <>):

```json
{
  "meta": {
    "project": "Agente IA Local",
    "prompt_kind": "task_template",
    "prompt_id": "TASK_TEMPLATE_V1"
  },
  "context": {
    "workspace_reference": "@workspace",
    "explicit_references": [
      "#Reglas.IA.md",
      "#Readme.UX.md",
      "<ADD: #File.cs | #File.xaml | #MethodName | #output | URL>"
    ]
  },
  "objective": "<ONE_SENTENCE_OBJECTIVE>",
  "constraints": {
    "must": [
      "SOLID + Clean Architecture + Onion (dependencies inward)",
      "MVVM for WPF changes",
      "Keep build stable in Visual Studio 2026 (Experimental Instance)",
      "Response JSON only and list created/modified/deleted items"
    ],
    "must_not": [
      "Do not modify: *.vsix, *.vsct, *.vsixmanifest, *.csproj, *.sln",
      "Do not use scripts or external automation",
      "Do not expand scope beyond objective"
    ]
  },
  "steps": [
    {
      "step": 1,
      "action": "Analyze referenced context and identify the minimal change set.",
      "requires_human_confirmation_before_applying": true
    },
    {
      "step": 2,
      "action": "Apply changes in the smallest possible number of files.",
      "requires_human_confirmation_before_next_step": true
    },
    {
      "step": 3,
      "action": "Summarize changes and point to NEW markers with IDs.",
      "requires_human_confirmation_before_finish": false
    }
  ],
  "acceptance_criterion": "<SINGLE_VERIFIABLE_CRITERION>",
  "response_format": {
    "json_only": true,
    "required_keys": [
      "status",
      "summary",
      "changes",
      "new_markers",
      "next_step"
    ]
  }
}

```

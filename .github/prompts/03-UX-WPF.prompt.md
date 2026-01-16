---
name: 'ux-wpf'
description: 'Cambio UX WPF/XAML (JSON tecnico)'
agent: 'agent'
argument-hint: 'ux_goal="..." refs="#Readme.UX.md #Control.xaml ..."'
---

Ejecuta este prompt y responde SOLO con JSON valido (sin texto adicional).
Si falta contexto: pide #File, #output, @workspace y marca status="needs_confirmation".

Plantilla JSON (rellena placeholders entre <>):

```json
{
  "meta": {
    "project": "Agente IA Local",
    "prompt_kind": "ux_wpf",
    "prompt_id": "UX_WPF_V1"
  },
  "context": {
    "workspace_reference": "@workspace",
    "explicit_references": [
      "#Readme.UX.md",
      "<ADD: #AgenteIALocalControl.xaml>",
      "<ADD: #AgenteIALocalControl.xaml.cs (if needed)>"
    ],
    "ux_goal": "<ONE_SENTENCE_UX_GOAL>"
  },
  "objective": "Implement the UX change exactly as specified in ux_goal, keeping VS dark theme consistency.",
  "constraints": {
    "must": [
      "No blocking calls on UI thread",
      "State transitions must reflect immediately in UI via proper bindings",
      "Keep XAML styles consistent with existing resources"
    ],
    "must_not": [
      "Do not introduce new UI frameworks",
      "Do not modify VSIX critical files"
    ]
  },
  "steps": [
    {
      "step": 1,
      "action": "Identify the minimal XAML + ViewModel/code-behind changes required.",
      "requires_human_confirmation_before_applying": true
    },
    {
      "step": 2,
      "action": "Apply changes and ensure bindings refresh (PropertyChanged for dependent properties).",
      "requires_human_confirmation_before_next_step": true
    }
  ],
  "acceptance_criterion": "<SINGLE_VERIFIABLE_CRITERION (e.g., 'When state becomes Running, header label shows Running and button shows Stop without needing reopen')>",
  "response_format": {
    "json_only": true,
    "must_list_changes": true
  }
}

```

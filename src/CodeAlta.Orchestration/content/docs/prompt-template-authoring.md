# Prompt Template Authoring

A prompt template is an optional `template.yml` file under a `prompts/` root. It selects default system/agent prompt ids and toggles generated prompt parts.

```yaml
version: 1
system: default
agent: default
skills: true
project_context: true
runtime_context: true
tool_guidance: true
```

The file is not a resource index and should not list paths. Use `system` for the system prompt id and `agent` for the selectable agent prompt id. The older `developer` key is still accepted for compatibility.

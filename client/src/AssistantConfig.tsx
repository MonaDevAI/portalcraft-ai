import { ChangeEvent, useMemo, useState } from "react";

type LookupKeyDraft = {
  id: string;
  label: string;
  example: string;
  aliases: string;
};

type BusinessEntityDraft = {
  id: string;
  label: string;
  group: string;
  aliases: string;
  requestRoute: string;
  requestActionLabel: string;
  routeParameters: string;
};

type AssistantConfigurationDraft = {
  assistantName: string;
  lookupKeys: LookupKeyDraft[];
  businessEntities: BusinessEntityDraft[];
};

type PortalCraftAssistantConfiguration = {
  assistantName: string;
  lookupKeys: Array<{
    id: string;
    label: string;
    example: string;
    aliases: string[];
  }>;
  businessEntities: Array<{
    id: string;
    label: string;
    group: string;
    aliases: string[];
    requestRoute: string;
    requestActionLabel?: string;
    routeParameters: Record<string, string>;
  }>;
};

const initialDraft: AssistantConfigurationDraft = {
  assistantName: "Portal Assistant",
  lookupKeys: [
    {
      id: "requestId",
      label: "Request ID",
      example: "REQ-100",
      aliases: "request, requests, request id",
    },
    {
      id: "validatorCRNumber",
      label: "Validator CR",
      example: "VCR-100",
      aliases: "validator cr, validator change request",
    },
  ],
  businessEntities: [
    {
      id: "exampleEntity",
      label: "Example entity",
      group: "Business entities",
      aliases: "example entity",
      requestRoute: "/search-requests",
      requestActionLabel: "",
      routeParameters: "",
    },
  ],
};

function splitAliases(value: string): string[] {
  return value
    .split(",")
    .map(alias => alias.trim())
    .filter(Boolean);
}

function parseRouteParameters(value: string): Record<string, string> {
  return Object.fromEntries(
    value
      .split(/\r?\n/)
      .map(line => line.trim())
      .filter(Boolean)
      .map(line => {
        const separator = line.indexOf("=");
        if (separator <= 0 || separator === line.length - 1) {
          throw new Error(`Route parameter "${line}" must use key=value.`);
        }
        return [line.slice(0, separator).trim(), line.slice(separator + 1).trim()];
      })
  );
}

function buildConfiguration(
  draft: AssistantConfigurationDraft
): PortalCraftAssistantConfiguration {
  return {
    assistantName: draft.assistantName.trim(),
    lookupKeys: draft.lookupKeys.map(key => ({
      id: key.id.trim(),
      label: key.label.trim(),
      example: key.example.trim(),
      aliases: splitAliases(key.aliases),
    })),
    businessEntities: draft.businessEntities.map(entity => {
      const configuration: PortalCraftAssistantConfiguration["businessEntities"][number] = {
        id: entity.id.trim(),
        label: entity.label.trim(),
        group: entity.group.trim(),
        aliases: splitAliases(entity.aliases),
        requestRoute: entity.requestRoute.trim(),
        routeParameters: parseRouteParameters(entity.routeParameters),
      };
      if (entity.requestActionLabel.trim()) {
        configuration.requestActionLabel = entity.requestActionLabel.trim();
      }
      return configuration;
    }),
  };
}

function validateDraft(draft: AssistantConfigurationDraft): string[] {
  const errors: string[] = [];
  if (!draft.assistantName.trim()) errors.push("Assistant name is required.");
  if (draft.lookupKeys.length === 0) errors.push("Add at least one lookup key.");
  if (draft.businessEntities.length === 0) errors.push("Add at least one business entity.");

  const lookupIds = new Set<string>();
  draft.lookupKeys.forEach((key, index) => {
    const prefix = `Lookup key ${index + 1}`;
    if (!key.id.trim()) errors.push(`${prefix} ID is required.`);
    if (!key.label.trim()) errors.push(`${prefix} label is required.`);
    if (splitAliases(key.aliases).length === 0) errors.push(`${prefix} needs an alias.`);
    const normalizedId = key.id.trim().toLowerCase();
    if (normalizedId && lookupIds.has(normalizedId)) errors.push(`${prefix} ID must be unique.`);
    lookupIds.add(normalizedId);
  });

  const entityIds = new Set<string>();
  draft.businessEntities.forEach((entity, index) => {
    const prefix = `Business entity ${index + 1}`;
    if (!entity.id.trim()) errors.push(`${prefix} ID is required.`);
    if (!entity.label.trim()) errors.push(`${prefix} label is required.`);
    if (!entity.group.trim()) errors.push(`${prefix} group is required.`);
    if (splitAliases(entity.aliases).length === 0) errors.push(`${prefix} needs an alias.`);
    if (!entity.requestRoute.startsWith("/") || entity.requestRoute.startsWith("//")) {
      errors.push(`${prefix} route must start with one slash.`);
    }
    const normalizedId = entity.id.trim().toLowerCase();
    if (normalizedId && entityIds.has(normalizedId)) errors.push(`${prefix} ID must be unique.`);
    entityIds.add(normalizedId);
    try {
      parseRouteParameters(entity.routeParameters);
    } catch (error) {
      errors.push(error instanceof Error ? `${prefix}: ${error.message}` : `${prefix} has invalid route parameters.`);
    }
  });
  return errors;
}

function readString(value: unknown, name: string): string {
  if (typeof value !== "string") throw new Error(`${name} must be a string.`);
  return value;
}

function importDraft(value: unknown): AssistantConfigurationDraft {
  if (!value || typeof value !== "object") throw new Error("Configuration must be a JSON object.");
  const configuration = value as Record<string, unknown>;
  if (!Array.isArray(configuration.lookupKeys) || !Array.isArray(configuration.businessEntities)) {
    throw new Error("Configuration requires lookupKeys and businessEntities arrays.");
  }
  return {
    assistantName: readString(configuration.assistantName, "assistantName"),
    lookupKeys: configuration.lookupKeys.map((item, index) => {
      if (!item || typeof item !== "object") throw new Error(`lookupKeys[${index}] must be an object.`);
      const key = item as Record<string, unknown>;
      if (!Array.isArray(key.aliases)) throw new Error(`lookupKeys[${index}].aliases must be an array.`);
      return {
        id: readString(key.id, `lookupKeys[${index}].id`),
        label: readString(key.label, `lookupKeys[${index}].label`),
        example: readString(key.example ?? "", `lookupKeys[${index}].example`),
        aliases: key.aliases.map(alias => readString(alias, "lookup alias")).join(", "),
      };
    }),
    businessEntities: configuration.businessEntities.map((item, index) => {
      if (!item || typeof item !== "object") throw new Error(`businessEntities[${index}] must be an object.`);
      const entity = item as Record<string, unknown>;
      if (!Array.isArray(entity.aliases)) throw new Error(`businessEntities[${index}].aliases must be an array.`);
      const routeParameters = entity.routeParameters ?? {};
      if (!routeParameters || typeof routeParameters !== "object" || Array.isArray(routeParameters)) {
        throw new Error(`businessEntities[${index}].routeParameters must be an object.`);
      }
      return {
        id: readString(entity.id, `businessEntities[${index}].id`),
        label: readString(entity.label, `businessEntities[${index}].label`),
        group: readString(entity.group, `businessEntities[${index}].group`),
        aliases: entity.aliases.map(alias => readString(alias, "business entity alias")).join(", "),
        requestRoute: readString(entity.requestRoute, `businessEntities[${index}].requestRoute`),
        requestActionLabel: readString(
          entity.requestActionLabel ?? "",
          `businessEntities[${index}].requestActionLabel`
        ),
        routeParameters: Object.entries(routeParameters)
          .map(([key, parameterValue]) => `${key}=${readString(parameterValue, `routeParameters.${key}`)}`)
          .join("\n"),
      };
    }),
  };
}

export function AssistantConfig() {
  const [draft, setDraft] = useState<AssistantConfigurationDraft>(initialDraft);
  const [status, setStatus] = useState("");
  const errors = useMemo(() => validateDraft(draft), [draft]);
  const json = useMemo(() => {
    if (errors.length > 0) return "";
    return JSON.stringify(buildConfiguration(draft), null, 2);
  }, [draft, errors]);

  function updateLookup(index: number, field: keyof LookupKeyDraft, value: string) {
    setDraft(current => ({
      ...current,
      lookupKeys: current.lookupKeys.map((key, keyIndex) =>
        keyIndex === index ? { ...key, [field]: value } : key
      ),
    }));
    setStatus("");
  }

  function updateEntity(index: number, field: keyof BusinessEntityDraft, value: string) {
    setDraft(current => ({
      ...current,
      businessEntities: current.businessEntities.map((entity, entityIndex) =>
        entityIndex === index ? { ...entity, [field]: value } : entity
      ),
    }));
    setStatus("");
  }

  async function copyJson() {
    await navigator.clipboard.writeText(json);
    setStatus("Configuration copied.");
  }

  function downloadJson() {
    const url = URL.createObjectURL(new Blob([json], { type: "application/json" }));
    const link = document.createElement("a");
    link.href = url;
    link.download = "portalcraft-assistant.json";
    link.click();
    URL.revokeObjectURL(url);
    setStatus("Downloaded portalcraft-assistant.json.");
  }

  function importJson(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => {
      try {
        setDraft(importDraft(JSON.parse(String(reader.result))));
        setStatus(`Imported ${file.name}.`);
      } catch (error) {
        setStatus(error instanceof Error ? error.message : "Could not import configuration.");
      }
    };
    reader.onerror = () => setStatus(`Could not read ${file.name}.`);
    reader.readAsText(file);
    event.target.value = "";
  }

  return (
    <section className="assistant-config">
      <div className="config-intro">
        <div>
          <h2>Assistant requirements</h2>
          <p>Define how PortalCraft asks for identifiers and opens matching requests in an existing portal.</p>
        </div>
        <label className="secondary-button">
          Import JSON
          <input type="file" accept="application/json,.json" onChange={importJson} />
        </label>
      </div>

      <label className="field">
        <span>Assistant name</span>
        <input
          aria-label="Assistant name"
          value={draft.assistantName}
          onChange={event => setDraft(current => ({ ...current, assistantName: event.target.value }))}
        />
      </label>

      <div className="config-section-heading">
        <div>
          <h3>Lookup keys</h3>
          <p>Identifiers users can provide, such as Request ID or Validator CR.</p>
        </div>
        <button
          className="secondary-button"
          onClick={() => setDraft(current => ({
            ...current,
            lookupKeys: [...current.lookupKeys, { id: "", label: "", example: "", aliases: "" }],
          }))}
        >
          Add lookup key
        </button>
      </div>
      <div className="config-list">
        {draft.lookupKeys.map((key, index) => (
          <article className="config-card" key={`lookup-${index}`}>
            <div className="config-card-title">
              <strong>Lookup key {index + 1}</strong>
              <button
                aria-label={`Remove lookup key ${index + 1}`}
                onClick={() => setDraft(current => ({
                  ...current,
                  lookupKeys: current.lookupKeys.filter((_, keyIndex) => keyIndex !== index),
                }))}
              >
                Remove
              </button>
            </div>
            <div className="form-grid">
              <label className="field"><span>ID</span><input aria-label={`Lookup key ${index + 1} ID`} value={key.id} onChange={event => updateLookup(index, "id", event.target.value)} /></label>
              <label className="field"><span>Display label</span><input aria-label={`Lookup key ${index + 1} label`} value={key.label} onChange={event => updateLookup(index, "label", event.target.value)} /></label>
              <label className="field"><span>Example</span><input aria-label={`Lookup key ${index + 1} example`} value={key.example} onChange={event => updateLookup(index, "example", event.target.value)} /></label>
              <label className="field field-wide"><span>Aliases, comma separated</span><input aria-label={`Lookup key ${index + 1} aliases`} value={key.aliases} onChange={event => updateLookup(index, "aliases", event.target.value)} /></label>
            </div>
          </article>
        ))}
      </div>

      <div className="config-section-heading">
        <div>
          <h3>Business entities and portal routes</h3>
          <p>Each entity becomes a clarification choice and a safe highlighted-request action.</p>
        </div>
        <button
          className="secondary-button"
          onClick={() => setDraft(current => ({
            ...current,
            businessEntities: [...current.businessEntities, {
              id: "",
              label: "",
              group: "",
              aliases: "",
              requestRoute: "",
              requestActionLabel: "",
              routeParameters: "",
            }],
          }))}
        >
          Add business entity
        </button>
      </div>
      <div className="config-list">
        {draft.businessEntities.map((entity, index) => (
          <article className="config-card" key={`entity-${index}`}>
            <div className="config-card-title">
              <strong>Business entity {index + 1}</strong>
              <button
                aria-label={`Remove business entity ${index + 1}`}
                onClick={() => setDraft(current => ({
                  ...current,
                  businessEntities: current.businessEntities.filter((_, entityIndex) => entityIndex !== index),
                }))}
              >
                Remove
              </button>
            </div>
            <div className="form-grid">
              <label className="field"><span>ID</span><input aria-label={`Business entity ${index + 1} ID`} value={entity.id} onChange={event => updateEntity(index, "id", event.target.value)} /></label>
              <label className="field"><span>Display label</span><input aria-label={`Business entity ${index + 1} label`} value={entity.label} onChange={event => updateEntity(index, "label", event.target.value)} /></label>
              <label className="field"><span>Clarification group</span><input aria-label={`Business entity ${index + 1} group`} value={entity.group} onChange={event => updateEntity(index, "group", event.target.value)} /></label>
              <label className="field"><span>Action label</span><input aria-label={`Business entity ${index + 1} action label`} value={entity.requestActionLabel} onChange={event => updateEntity(index, "requestActionLabel", event.target.value)} /></label>
              <label className="field field-wide"><span>Aliases, comma separated</span><input aria-label={`Business entity ${index + 1} aliases`} value={entity.aliases} onChange={event => updateEntity(index, "aliases", event.target.value)} /></label>
              <label className="field field-wide"><span>Existing portal request route</span><input aria-label={`Business entity ${index + 1} route`} value={entity.requestRoute} onChange={event => updateEntity(index, "requestRoute", event.target.value)} /></label>
              <label className="field field-wide">
                <span>Fixed route parameters, one key=value per line</span>
                <textarea aria-label={`Business entity ${index + 1} route parameters`} value={entity.routeParameters} onChange={event => updateEntity(index, "routeParameters", event.target.value)} />
              </label>
            </div>
          </article>
        ))}
      </div>

      <div className="config-output">
        <div className="config-output-heading">
          <div>
            <h3>Generated portalcraft-assistant.json</h3>
            <p>Save this file in the target repository and pass it to <code>assistant --assistant-config</code>.</p>
          </div>
          <div className="config-actions">
            <button className="secondary-button" disabled={!json} onClick={copyJson}>Copy JSON</button>
            <button className="primary-button" disabled={!json} onClick={downloadJson}>Download JSON</button>
          </div>
        </div>
        {errors.length > 0 ? (
          <div className="config-errors" role="alert">
            <strong>Complete these fields before exporting:</strong>
            <ul>{errors.map(error => <li key={error}>{error}</li>)}</ul>
          </div>
        ) : (
          <pre aria-label="Assistant configuration JSON">{json}</pre>
        )}
        {status && <p className="config-status" role="status">{status}</p>}
      </div>
    </section>
  );
}

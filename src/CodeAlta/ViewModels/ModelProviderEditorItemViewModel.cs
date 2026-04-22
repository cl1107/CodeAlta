using CodeAlta.Catalog;
using XenoAtom.Terminal.UI;

namespace CodeAlta.ViewModels;

internal sealed partial class ModelProviderEditorItemViewModel
{
    private readonly CodeAltaProviderDocument _source;
    private bool _isInitialized;

    private ModelProviderEditorItemViewModel(CodeAltaProviderDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
        ProviderKey = source.ProviderKey;
        IsReserved = string.Equals(source.ProviderKey, "codex", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(source.ProviderKey, "copilot", StringComparison.OrdinalIgnoreCase);
        Enabled = source.Enabled != false;
        ProviderType = source.ProviderType ?? "openai-chat";
        DisplayName = source.DisplayName;
        UseDefaultDisplayName = source.DisplayName is null;
        Model = source.Model;
        UseDefaultModel = source.Model is null;
        ReasoningEffort = source.ReasoningEffort ?? "high";
        UseDefaultReasoningEffort = source.ReasoningEffort is null;
        ApiKey = source.ApiKey;
        UseDefaultApiKey = source.ApiKey is null;
        ApiKeyEnv = source.ApiKeyEnv;
        UseDefaultApiKeyEnv = source.ApiKeyEnv is null;
        ApiUrl = source.ApiUrl;
        UseDefaultApiUrl = source.ApiUrl is null;
        OrganizationId = source.OrganizationId;
        UseDefaultOrganizationId = source.OrganizationId is null;
        ProjectId = source.ProjectId;
        UseDefaultProjectId = source.ProjectId is null;
        Project = source.Project;
        UseDefaultProject = source.Project is null;
        Location = source.Location;
        UseDefaultLocation = source.Location is null;
        ModelsDevProviderId = source.ModelsDevProviderId;
        UseDefaultModelsDevProviderId = source.ModelsDevProviderId is null;
        SingleModelId = source.SingleModelId;
        UseDefaultSingleModelId = source.SingleModelId is null;
        _isInitialized = true;
    }

    public event Action<ModelProviderEditorItemViewModel, bool>? Changed;

    [Bindable]
    public partial string? ProviderKey { get; set; }

    public bool IsReserved { get; }

    [Bindable]
    public partial bool Enabled { get; set; }

    [Bindable]
    public partial string ProviderType { get; set; }

    [Bindable]
    public partial string? DisplayName { get; set; }

    [Bindable]
    public partial bool UseDefaultDisplayName { get; set; }

    [Bindable]
    public partial string? Model { get; set; }

    [Bindable]
    public partial bool UseDefaultModel { get; set; }

    [Bindable]
    public partial string? ReasoningEffort { get; set; }

    [Bindable]
    public partial bool UseDefaultReasoningEffort { get; set; }

    [Bindable]
    public partial string? ApiKey { get; set; }

    [Bindable]
    public partial bool UseDefaultApiKey { get; set; }

    [Bindable]
    public partial string? ApiKeyEnv { get; set; }

    [Bindable]
    public partial bool UseDefaultApiKeyEnv { get; set; }

    [Bindable]
    public partial string? ApiUrl { get; set; }

    [Bindable]
    public partial bool UseDefaultApiUrl { get; set; }

    [Bindable]
    public partial string? OrganizationId { get; set; }

    [Bindable]
    public partial bool UseDefaultOrganizationId { get; set; }

    [Bindable]
    public partial string? ProjectId { get; set; }

    [Bindable]
    public partial bool UseDefaultProjectId { get; set; }

    [Bindable]
    public partial string? Project { get; set; }

    [Bindable]
    public partial bool UseDefaultProject { get; set; }

    [Bindable]
    public partial string? Location { get; set; }

    [Bindable]
    public partial bool UseDefaultLocation { get; set; }

    [Bindable]
    public partial string? ModelsDevProviderId { get; set; }

    [Bindable]
    public partial bool UseDefaultModelsDevProviderId { get; set; }

    [Bindable]
    public partial string? SingleModelId { get; set; }

    [Bindable]
    public partial bool UseDefaultSingleModelId { get; set; }

    public string Label => string.IsNullOrWhiteSpace(DisplayName) ? ProviderKey ?? string.Empty : DisplayName.Trim();

    public static ModelProviderEditorItemViewModel FromDocument(CodeAltaProviderDocument definition)
        => new(Clone(definition));

    public static ModelProviderEditorItemViewModel Create(string providerKey)
        => new(new CodeAltaProviderDocument
        {
            ProviderKey = providerKey,
            Enabled = false,
            ProviderType = "openai-chat",
        });

    public CodeAltaProviderDocument ToDocument()
    {
        var definition = Clone(_source);
        definition.ProviderKey = (ProviderKey ?? string.Empty).Trim().ToLowerInvariant();
        definition.Enabled = Enabled;
        definition.ProviderType = ProviderType;
        definition.DisplayName = UseDefaultDisplayName ? null : NormalizeText(DisplayName);
        definition.Model = UseDefaultModel ? null : NormalizeText(Model);
        definition.ReasoningEffort = UseDefaultReasoningEffort ? null : NormalizeText(ReasoningEffort);
        definition.ApiKey = UseDefaultApiKey ? null : NormalizeText(ApiKey);
        definition.ApiKeyEnv = UseDefaultApiKeyEnv ? null : NormalizeText(ApiKeyEnv);
        definition.ApiUrl = UseDefaultApiUrl ? null : NormalizeText(ApiUrl);
        definition.OrganizationId = UseDefaultOrganizationId ? null : NormalizeText(OrganizationId);
        definition.ProjectId = UseDefaultProjectId ? null : NormalizeText(ProjectId);
        definition.Project = UseDefaultProject ? null : NormalizeText(Project);
        definition.Location = UseDefaultLocation ? null : NormalizeText(Location);
        definition.ModelsDevProviderId = UseDefaultModelsDevProviderId ? null : NormalizeText(ModelsDevProviderId);
        definition.SingleModelId = UseDefaultSingleModelId ? null : NormalizeText(SingleModelId);
        return definition;
    }

    partial void OnProviderKeyChanged(string? value) => NotifyChanged();
    partial void OnEnabledChanged(bool value) => NotifyChanged();
    partial void OnProviderTypeChanged(string value) => NotifyChanged(rebuildEditor: true);
    partial void OnDisplayNameChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultDisplayNameChanged(bool value) => NotifyChanged();
    partial void OnModelChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultModelChanged(bool value) => NotifyChanged();
    partial void OnReasoningEffortChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultReasoningEffortChanged(bool value) => NotifyChanged();
    partial void OnApiKeyChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultApiKeyChanged(bool value) => NotifyChanged();
    partial void OnApiKeyEnvChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultApiKeyEnvChanged(bool value) => NotifyChanged();
    partial void OnApiUrlChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultApiUrlChanged(bool value) => NotifyChanged();
    partial void OnOrganizationIdChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultOrganizationIdChanged(bool value) => NotifyChanged();
    partial void OnProjectIdChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultProjectIdChanged(bool value) => NotifyChanged();
    partial void OnProjectChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultProjectChanged(bool value) => NotifyChanged();
    partial void OnLocationChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultLocationChanged(bool value) => NotifyChanged();
    partial void OnModelsDevProviderIdChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultModelsDevProviderIdChanged(bool value) => NotifyChanged();
    partial void OnSingleModelIdChanged(string? value) => NotifyChanged();
    partial void OnUseDefaultSingleModelIdChanged(bool value) => NotifyChanged();

    private static CodeAltaProviderDocument Clone(CodeAltaProviderDocument definition)
    {
        return new CodeAltaProviderDocument
        {
            ProviderKey = definition.ProviderKey,
            Enabled = definition.Enabled,
            DisplayName = definition.DisplayName,
            ProviderType = definition.ProviderType,
            Model = definition.Model,
            ReasoningEffort = definition.ReasoningEffort,
            ApiKey = definition.ApiKey,
            ApiKeyEnv = definition.ApiKeyEnv,
            ApiUrl = definition.ApiUrl,
            OrganizationId = definition.OrganizationId,
            ProjectId = definition.ProjectId,
            Project = definition.Project,
            Location = definition.Location,
            ModelsDevProviderId = definition.ModelsDevProviderId,
            SingleModelId = definition.SingleModelId,
            ExtraBody = definition.ExtraBody,
            Profile = definition.Profile,
            Compaction = definition.Compaction,
            ModelOverrides = definition.ModelOverrides,
        };
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void NotifyChanged(bool rebuildEditor = false)
    {
        if (!_isInitialized)
        {
            return;
        }

        Changed?.Invoke(this, rebuildEditor);
    }
}

namespace Content.Shared._Starlight.Implants.Components
{
    /// <summary>
    /// Component for Mind Control implants.
    /// </summary>
    [RegisterComponent]
    public sealed partial class MindControlImplantComponent : Component
    {
        //tracks the implants owner
        [DataField(required: false)] public EntityUid Master; 

    }
}
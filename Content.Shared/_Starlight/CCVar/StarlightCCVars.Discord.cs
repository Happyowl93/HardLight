using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;
public sealed partial class StarlightCCVars
{
    /// <summary>
    /// Discord oAuth
    /// </summary>

    public static readonly CVarDef<string> DiscordCallback =
        CVarDef.Create("discord.callback", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> Secret =
        CVarDef.Create("discord.secret", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
        
        
    /// <summary>
    /// Discord Webhooks
    /// </summary>
    
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", string.Empty, CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordAdminAutoLogWebhook =
        CVarDef.Create("discord.admin_autolog", "https://discord.com/api/webhooks/1475606920370061437/K926KA1AW6UUUGG6CxjWQjddoR5vZbxKCiI1bf0h4Ojslp3lVyC6cdUk9cVAUDaTfVC4", CVar.SERVERONLY);
}

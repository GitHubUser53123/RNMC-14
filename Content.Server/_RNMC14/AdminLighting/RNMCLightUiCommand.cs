using Content.Server._RNMC14.AdminLighting;
using Content.Server._RNMC14.AdminToggles;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RNMCLightUiCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly EuiManager _euiManager = default!;

        public override string Command => "rnmclightui";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            var ui = new LightingTabEui();
            _euiManager.OpenEui(ui, player);
        }
    }
}

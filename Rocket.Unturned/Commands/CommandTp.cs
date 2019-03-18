﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.API.Commands;
using Rocket.API.I18N;
using Rocket.API.Player;
using Rocket.API.User;
using Rocket.Core.Commands;
using Rocket.Core.I18N;
using Rocket.UnityEngine.Extensions;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Rocket.Unturned.Commands
{
    public class CommandTp : ICommand
    {
        public bool SupportsUser(IUser user) => user is UnturnedUser;
        public async Task ExecuteAsync(ICommandContext context)
        {
            ITranslationCollection translations = ((RocketUnturnedHost)context.Container.Resolve<IHost>()).ModuleTranslations;

            UnturnedPlayer player = ((UnturnedUser)context).Player;

            if (context.Parameters.Length != 1 && context.Parameters.Length != 3)
                throw new CommandWrongUsageException();

            if (player.Entity.Stance == EPlayerStance.DRIVING || player.Entity.Stance == EPlayerStance.SITTING)
                throw new CommandWrongUsageException(
                    await translations.GetAsync("command_generic_teleport_while_driving_error"));

            float? x = null;
            float? y = null;
            float? z = null;

            if (context.Parameters.Length == 3)
            {
                x = await context.Parameters.GetAsync<float>(0);
                y = await context.Parameters.GetAsync<float>(1);
                z = await context.Parameters.GetAsync<float>(2);
            }

            if (x != null)
            {
                player.Entity.Teleport(new System.Numerics.Vector3((float)x, (float)y, (float)z));
                await context.User.SendLocalizedMessageAsync(translations, "command_tp_teleport_private", null, (float)x + "," + (float)y + "," + (float)z);
                return;
            }

            if (await context.Parameters.GetAsync<IPlayer>(0) is UnturnedPlayer otherplayer && otherplayer != player)
            {
                player.Entity.Teleport(otherplayer);
                await context.User.SendLocalizedMessageAsync(translations, "command_tp_teleport_private", null, otherplayer.CharacterName);
                return;
            }

            Node item = LevelNodes.nodes.FirstOrDefault(n => n.type == ENodeType.LOCATION && ((LocationNode)n).name.ToLower().Contains((context.Parameters.GetAsync<string>(0).GetAwaiter().GetResult()).ToLower()));
            if (item != null)
            {
                Vector3 c = item.point + new Vector3(0f, 0.5f, 0f);
                player.Entity.Teleport(c.ToSystemVector());
                await context.User.SendLocalizedMessageAsync(translations, "command_tp_teleport_private", null, ((LocationNode)item).name);
                return;
            }

            await context.User.SendLocalizedMessageAsync(translations, "command_tp_failed_find_destination");
        }

        public string Name => "Tp";
        public string Summary => "Teleports you to another player or location.";
        public string Description => null;
        public string Permission => "Rocket.Unturned.Tp";
        public string Syntax => "<player | place | x y z>";
        public IChildCommand[] ChildCommands => null;
        public string[] Aliases => null;
    }
}
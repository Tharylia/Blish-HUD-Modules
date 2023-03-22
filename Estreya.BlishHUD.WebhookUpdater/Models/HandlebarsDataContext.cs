namespace Estreya.BlishHUD.WebhookUpdater.Models;

using Blish_HUD;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class HandlebarsDataContext
{
    public Gw2MumbleService mumble { get; set; }

    public APIContext api { get; set; }

    public class APIContext
    {
        public Account Account { get; set; }

        public Guild Guild { get; set; }

        public Character[] Characters { get; set; }

        public Character CurrentCharacter => this.Characters.FirstOrDefault(character => character.Name == GameService.Gw2Mumble.PlayerCharacter.Name);

        public Map Map { get; set; }
    }
}

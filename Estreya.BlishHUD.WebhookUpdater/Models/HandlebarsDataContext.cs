namespace Estreya.BlishHUD.WebhookUpdater.Models;

using Blish_HUD;
using Gw2Sharp.WebApi.V2.Models;
using System.Linq;

public class HandlebarsDataContext
{
    public Gw2MumbleService mumble => GameService.Gw2Mumble;

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
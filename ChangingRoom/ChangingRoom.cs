using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

public enum ComponentId
{
    FACE,
    BEARD,
    HAIRCUT,
    SHIRT,
    PANTS,
    HANDS,
    SHOES,
    EYES,
    ACCESSORIES,
    ITEMS,
    DECALS,
    COLLARS
}

public class ChangingRoom : Script
{
    private readonly Dictionary<Keys, Action> _hotkeys;
    private readonly Dictionary<string, Action<string[]>> _hotstrings;

    public ChangingRoom()
    {
        Tick += onTick;
        KeyUp += onKeyUp;
        KeyDown += onKeyDown;
        Interval = 100;
        _hotstrings = new Dictionary<string, Action<string[]>>();
        _hotstrings.Add("set_player_model", ActionSetPlayerModel);
        _hotstrings.Add("set_component_variation", ActionSetComponentVariation);
        _hotkeys = new Dictionary<Keys, Action>();
        _hotkeys.Add(Keys.Decimal, () =>
        {
            string result = Game.GetUserInput(80);
            if (result == null)
                return;
            String[] command = result.Split(' '); ;
            if (_hotstrings.ContainsKey(command[0]))
                _hotstrings[command[0]](command.Skip(1).ToArray());
            else
                UI.Notify("unknown command");
        });
    }

    private void onTick(object sender, EventArgs e)
    {
    }

    private void onKeyDown(object sender, KeyEventArgs e)
    {
    }

    private void onKeyUp(object sender, KeyEventArgs e)
    {
        foreach (var hotkey in _hotkeys.Keys.Where(hotkey => e.KeyCode == hotkey))
            _hotkeys[hotkey]();
    }

    public void ActionSetPlayerModel(string[] args)
    {
        if (args.Count() != 1)
        {
            UI.Notify("expected a model name");
            return;
        }
        var characterModel = new Model(args[0]);
        characterModel.Request(500);

        // Check the model is valid
        if (characterModel.IsInCdImage && characterModel.IsValid)
        {
            // If the model isn't loaded, wait until it is
            while (!characterModel.IsLoaded) Script.Wait(100);

            // Set the player's model
            Function.Call(Hash.SET_PLAYER_MODEL, Game.Player, characterModel.Hash);
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character.Handle);
        }
        else
        {
            UI.Notify("could not request model (invalid name?)");
        }

        // Delete the model from memory after we've assigned it
        characterModel.MarkAsNoLongerNeeded();
    }

    public void ActionSetComponentVariation(string[] args)
    {
        if (args.Count() != 3)
        {
            UI.Notify("expected component, drawable id, and texture id");
            return;
        }
        ComponentId componentId;
        int drawableId;
        int textureId;
        if (!ComponentId.TryParse(args[0], out componentId))
        {
            UI.Notify("invalid component id");
            return;
        }
        if (!int.TryParse(args[1], out drawableId))
        {
            UI.Notify("invalid drawable id");
            return;
        }
        if (!int.TryParse(args[2], out textureId))
        {
            UI.Notify("invalid texture id");
            return;
        }
        var drawableNum = Function.Call<int>(Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, Game.Player.Character.Handle, (int)componentId);
        var textureNum = Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, Game.Player.Character.Handle, (int)componentId);
        if (drawableId >= drawableNum || drawableId < 0)
        {
            UI.Notify(String.Format("drawable id range is 0 - {0}", drawableNum - 1));
            return;
        }
        if (textureId >= textureNum || textureId < 0)
        {
            UI.Notify(String.Format("texture id range is 0 - {0}", textureNum - 1));
            return;
        }
        NativeSetComponentVariation(componentId, drawableId, textureId);
    }

    public void NativeSetComponentVariation(ComponentId componentId, int drawableId, int textureId)
    {
        Function.Call(
            Hash.SET_PED_COMPONENT_VARIATION,
            Game.Player.Character.Handle,
            (int)componentId,
            drawableId,
            textureId,
            2); // 2 = paletteId
        UI.Notify(String.Format("variation = {0} {1} {2}", componentId, drawableId, textureId));
    }
}

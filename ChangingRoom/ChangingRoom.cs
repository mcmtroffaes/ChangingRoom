using GTA;
using GTA.Native;
using NativeUI;
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
    public static PedHash[] modelsPlayer = {
        PedHash.Michael,
        PedHash.Franklin,
        PedHash.Trevor,
    };

    public static PedHash[] modelsMission = {
        PedHash.Abigail,
        PedHash.AmandaTownley,
        PedHash.Andreas,
        PedHash.Ashley,
        PedHash.Ballasog,
        PedHash.Bankman,
        PedHash.Barry,
        PedHash.Bestmen,
        PedHash.Beverly,
        PedHash.Brad,
        PedHash.Bride,
    };

    private readonly Dictionary<string, PedHash> _pedhash;

    private UIMenu menuMain;
    private UIMenu menuModel;
    private UIMenu menuModelPlayer;
    private UIMenu menuModelMission;
    private UIMenu menuOutfit;
    private UIMenuListItem outfitComponentListItem;
    private UIMenuListItem outfitDrawableListItem;
    private UIMenuListItem outfitTextureListItem;

    private MenuPool _menuPool;

    private readonly Dictionary<Keys, Action> _hotkeys;
    private readonly Dictionary<string, Action<string[]>> _hotstrings;

    public void AddCategoryToMenuModel(string name, PedHash[] models)
    {
        var submenu = new UIMenu("Changing Room", name);
        var menuItem = new UIMenuItem(name);
        menuModel.AddItem(menuItem);
        _menuPool.Add(submenu);
        foreach (PedHash model in models)
        {
            var subitem = new UIMenuItem(model.ToString());
            submenu.AddItem(subitem);
        }
        submenu.RefreshIndex();
        menuModel.BindMenuToItem(submenu, menuItem);
        submenu.OnItemSelect += modelOnItemSelect;
    }

    public ChangingRoom()
    {
        Tick += onTick;
        KeyUp += onKeyUp;
        KeyDown += onKeyDown;

        _pedhash = new Dictionary<string, PedHash>();
        foreach (PedHash model in Enum.GetValues(typeof(PedHash))) _pedhash[model.ToString()] = model;

        _menuPool = new MenuPool();

        menuMain = new UIMenu("Changing Room", "Main Menu");
        _menuPool.Add(menuMain);
        var menuItemModel = new UIMenuItem("Change Model");
        menuMain.AddItem(menuItemModel);
        var menuItemOutfit = new UIMenuItem("Change Outfit");
        menuMain.AddItem(menuItemOutfit);
        menuMain.RefreshIndex();

        menuModel = new UIMenu("Changing Room", "Model Categories");
        _menuPool.Add(menuModel);
        AddCategoryToMenuModel("Player Characters", modelsPlayer);
        AddCategoryToMenuModel("Mission Characters", modelsMission);
        menuModel.RefreshIndex();
        menuMain.BindMenuToItem(menuModel, menuItemModel);

        // old interface, to be removed
        _hotstrings = new Dictionary<string, Action<string[]>>();
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
        _menuPool.ProcessMenus();
    }

    private void onKeyDown(object sender, KeyEventArgs e)
    {
    }

    private void onKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5 && !_menuPool.IsAnyMenuOpen())
        {
            menuMain.Visible = !menuMain.Visible;
        }

        foreach (var hotkey in _hotkeys.Keys.Where(hotkey => e.KeyCode == hotkey))
            _hotkeys[hotkey]();
    }

    public void modelOnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
    {
        var characterModel = new Model(_pedhash[selectedItem.Text]);
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
            UI.Notify("could not request model");
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

using GTA;
using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

public enum ComponentId
{
    Face,
    Beard,
    Haircut,
    Shirt,
    Pants,
    Hands,
    Shoes,
    Eyes,
    Accessories,
    Items,
    Decals,
    Collars,
}

public enum ComponentWhat
{
    Drawable,
    Texture,
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

    public static Dictionary<ComponentWhat, int> NUM_COMPONENT_WHAT = new Dictionary<ComponentWhat, int>
    {
        { ComponentWhat.Drawable, 50 },
        { ComponentWhat.Texture, 50 }
    };

    private readonly Dictionary<string, PedHash> _pedhash;
    private readonly Dictionary<string, ComponentId> _componentid;
    private readonly Dictionary<string, ComponentWhat> _componentwhat;

    private UIMenu menuMain;
    private MenuPool _menuPool;

    public void AddCategoryToMenu(UIMenu menu, string name, PedHash[] models)
    {
        var submenu = new UIMenu("Changing Room", name);
        var menuItem = new UIMenuItem(name);
        menu.AddItem(menuItem);
        _menuPool.Add(submenu);
        foreach (PedHash model in models)
        {
            var subitem = new UIMenuItem(model.ToString());
            submenu.AddItem(subitem);
        }
        submenu.RefreshIndex();
        menu.BindMenuToItem(submenu, menuItem);
        submenu.OnItemSelect += modelOnItemSelect;
    }

    public void AddComponentToMenu(UIMenu menu, ComponentId componentId, ComponentWhat componentWhat)
    {
        var menuItem = new UIMenuListItem(
            componentId.ToString() + " " + componentWhat.ToString(),
            Enumerable.Range(0, NUM_COMPONENT_WHAT[componentWhat]).Cast<dynamic>().ToList(),
            0);
        menu.AddItem(menuItem);
    }

    public ChangingRoom()
    {
        Tick += onTick;
        KeyUp += onKeyUp;
        KeyDown += onKeyDown;

        _pedhash = new Dictionary<string, PedHash>();
        _componentid = new Dictionary<string, ComponentId>();
        _componentwhat = new Dictionary<string, ComponentWhat>();
        foreach (PedHash x in Enum.GetValues(typeof(PedHash))) _pedhash[x.ToString()] = x;
        foreach (ComponentId x in Enum.GetValues(typeof(ComponentId))) _componentid[x.ToString()] = x;
        foreach (ComponentWhat x in Enum.GetValues(typeof(ComponentWhat))) _componentwhat[x.ToString()] = x;

        _menuPool = new MenuPool();

        menuMain = new UIMenu("Changing Room", "Main Menu");
        _menuPool.Add(menuMain);
        var menuItemModel = new UIMenuItem("Change Model");
        menuMain.AddItem(menuItemModel);
        var menuItemOutfit = new UIMenuItem("Change Outfit");
        menuMain.AddItem(menuItemOutfit);
        menuMain.RefreshIndex();

        var menuModel = new UIMenu("Changing Room", "Model Categories");
        _menuPool.Add(menuModel);
        AddCategoryToMenu(menuModel, "Player Characters", modelsPlayer);
        AddCategoryToMenu(menuModel, "Mission Characters", modelsMission);
        menuModel.RefreshIndex();
        menuMain.BindMenuToItem(menuModel, menuItemModel);

        var menuOutfit = new UIMenu("Changing Room", "Outfit Categories");
        _menuPool.Add(menuOutfit);
        foreach (ComponentId componentId in Enum.GetValues(typeof(ComponentId)))
            foreach (ComponentWhat componentWhat in Enum.GetValues(typeof(ComponentWhat)))
                AddComponentToMenu(menuOutfit, componentId, componentWhat);
        menuOutfit.OnListChange += outfitOnListChange;
        menuOutfit.RefreshIndex();
        menuMain.BindMenuToItem(menuOutfit, menuItemOutfit);

        menuMain.OnItemSelect += mainOnItemSelect;
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
            menuMain.Visible = !menuMain.Visible;
    }

    public void mainOnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
    {
        if (selectedItem.Text == "Change Outfit")
        {
            // we need to get the ids of every item and set the list indices accordingly
            foreach(UIMenuListItem item in sender.Children[selectedItem].MenuItems)
            {
                var itemParts = item.Text.Split(' ');
                var componentId = _componentid[itemParts[0]];
                var componentWhat = _componentwhat[itemParts[1]];
                var id = GetPedVariation(componentId, componentWhat);
                item.Index = id;
            }
        }
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

    public int GetNumPedVariations(ComponentId componentId, ComponentWhat componentWhat)
    {
        GTA.Native.Hash hash = (componentWhat == ComponentWhat.Drawable) ? Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS : Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS;
        return Function.Call<int>(hash, Game.Player.Character.Handle, (int)componentId);
    }

    public int GetPedVariation(ComponentId componentId, ComponentWhat componentWhat)
    {
        GTA.Native.Hash hash = (componentWhat == ComponentWhat.Drawable) ? Hash.GET_PED_DRAWABLE_VARIATION : Hash.GET_PED_TEXTURE_VARIATION;
        return Function.Call<int>(hash, Game.Player.Character.Handle, (int)componentId);
    }

    public void outfitOnListChange(UIMenu sender, UIMenuListItem listItem, int newIndex)
    {
        var itemParts = listItem.Text.Split(' ');
        ComponentId componentId = _componentid[itemParts[0]];
        ComponentWhat componentWhat = _componentwhat[itemParts[1]];
        int id = int.Parse(listItem.IndexToItem(newIndex).ToString());
        var currentId = new Dictionary<ComponentWhat, int>
        {
            { ComponentWhat.Drawable, GetPedVariation(componentId, ComponentWhat.Drawable) },
            { ComponentWhat.Texture, GetPedVariation(componentId, ComponentWhat.Texture) },
        };
        // we need to ensure that the new id is valid as the menu has more items than number of ids supported by the game
        // GET_NUMBER_OF_PED_..._VARIATIONS sometimes returns 0, in this case there is also exactly one variation
        var num = Math.Max(0, GetNumPedVariations(componentId, componentWhat) - 1);
        if (id > num)
        {
            // wrap the index depending on whether user scrolled forward or backward
            id = (listItem.Index == NUM_COMPONENT_WHAT[componentWhat] - 1) ? num : 0;
            listItem.Index = id;
        }
        currentId[componentWhat] = id;
        NativeSetComponentVariation(componentId, currentId[ComponentWhat.Drawable], currentId[ComponentWhat.Texture]);
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
    }
}

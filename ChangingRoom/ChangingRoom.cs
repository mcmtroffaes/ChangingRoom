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
    static int UI_LIST_MAX = 50;

    private readonly Dictionary<string, PedHash> _pedhash;
    private readonly Dictionary<string, ComponentId> _componentid;
    private readonly Dictionary<string, ComponentWhat> _componentwhat;

    private MenuPool menuPool;
    private UIMenu menuMain;

    public UIMenu AddSubMenu(UIMenu menu, string name)
    {
        var item = new UIMenuItem(name);
        menu.AddItem(item);
        var submenu = new UIMenu(menu.Title.Caption, name);
        menuPool.Add(submenu);
        menu.BindMenuToItem(submenu, item);
        return submenu;
    }

    public void AddStorymodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Story Mode");
        AddStorymodeModelToMenu(submenu);
        AddStorymodeOutfitToMenu(submenu);
        submenu.RefreshIndex();
    }

    public void AddStorymodeModelToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Change Model");
        foreach (var pedItem in ChangingRoomPeds.peds)
            AddCategoryToMenu(submenu, pedItem.Item1, pedItem.Item2);
        submenu.RefreshIndex();
    }

    public void AddCategoryToMenu(UIMenu menu, string name, PedHash[] models)
    {
        var submenu = AddSubMenu(menu, name);
        foreach (PedHash model in models)
        {
            var subitem = new UIMenuItem(model.ToString());
            submenu.AddItem(subitem);
        }
        submenu.RefreshIndex();
        submenu.OnItemSelect += OnItemSelectModel;
    }

    public void AddStorymodeOutfitToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Change Outfit");
        foreach (ComponentId componentId in Enum.GetValues(typeof(ComponentId)))
            foreach (ComponentWhat componentWhat in Enum.GetValues(typeof(ComponentWhat)))
                AddComponentToMenu(submenu, componentId, componentWhat);
        submenu.RefreshIndex();
        menu.OnItemSelect += OnItemSelectOutfit;
        submenu.OnListChange += OnListChangeOutfit;
    }

    public void AddComponentToMenu(UIMenu menu, ComponentId componentId, ComponentWhat componentWhat)
    {
        var item = new UIMenuListItem(
            componentId.ToString() + " " + componentWhat.ToString(),
            Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList(),
            0);
        menu.AddItem(item);
    }

    public void AddFreemodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Free Mode");
        AddFreemodeModelToMenu(submenu);
        AddFreemodeOutfitToMenu(submenu);
        submenu.RefreshIndex();
    }

    public void AddFreemodeModelToMenu(UIMenu menu)
    {
        menu.AddItem(new UIMenuItem("Male"));
        menu.AddItem(new UIMenuItem("Female"));
        menu.OnItemSelect += (sender, selectedItem, index) =>
        {
            if (selectedItem.Text == "Male")
                NativeSetPlayerModel(new Model(PedHash.FreemodeMale01));
            else if (selectedItem.Text == "Female")
                NativeSetPlayerModel(new Model(PedHash.FreemodeFemale01));
        };
    }

    public void AddFreemodeOutfitToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Change Outfit");
        // TODO needs different component ids
        foreach (ComponentId componentId in Enum.GetValues(typeof(ComponentId)))
            foreach (ComponentWhat componentWhat in Enum.GetValues(typeof(ComponentWhat)))
                AddComponentToMenu(submenu, componentId, componentWhat);
        submenu.RefreshIndex();
        menu.OnItemSelect += OnItemSelectOutfit;
        submenu.OnListChange += OnListChangeOutfit;
    }

    public void AddExpertmodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Expert Mode");
        AddExpertmodeCompvarToMenu(submenu);
        submenu.RefreshIndex();
    }

    public void AddExpertmodeCompvarToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "SET_PED_COMPONENT_VARIATION");
        submenu.AddItem(
            new UIMenuListItem(
                "Component Id",
                Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList(),
                0));
        submenu.AddItem(
            new UIMenuListItem(
                "Drawable Id",
                Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList(),
                0));
        submenu.AddItem(
            new UIMenuListItem(
                "Texture Id",
                Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList(),
                0));
        submenu.AddItem(new UIMenuItem("Set"));
        submenu.OnItemSelect += OnItemSelectCompvar;
        submenu.RefreshIndex();
    }

    public ChangingRoom()
    {
        Tick += OnTick;
        KeyUp += OnKeyUp;
        KeyDown += OnKeyDown;

        _pedhash = new Dictionary<string, PedHash>();
        _componentid = new Dictionary<string, ComponentId>();
        _componentwhat = new Dictionary<string, ComponentWhat>();
        foreach (PedHash x in Enum.GetValues(typeof(PedHash))) _pedhash[x.ToString()] = x;
        foreach (ComponentId x in Enum.GetValues(typeof(ComponentId))) _componentid[x.ToString()] = x;
        foreach (ComponentWhat x in Enum.GetValues(typeof(ComponentWhat))) _componentwhat[x.ToString()] = x;

        menuPool = new MenuPool();
        menuMain = new UIMenu("Changing Room", "Main Menu");
        menuPool.Add(menuMain);
        AddStorymodeToMenu(menuMain);
        AddFreemodeToMenu(menuMain);
        AddExpertmodeToMenu(menuMain);
        menuMain.RefreshIndex();
    }

    private void OnTick(object sender, EventArgs e)
    {
        menuPool.ProcessMenus();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5 && !menuPool.IsAnyMenuOpen())
            menuMain.Visible = !menuMain.Visible;
    }

    public void OnItemSelectOutfit(UIMenu sender, UIMenuItem selectedItem, int index)
    {
        if (selectedItem.Text == "Change Outfit")
        {
            // we need to get the ids of every item and set the list indices accordingly
            foreach(UIMenuListItem item in sender.Children[selectedItem].MenuItems)
            {
                var itemParts = item.Text.Split(' ');
                var componentId = _componentid[itemParts[0]];
                var componentWhat = _componentwhat[itemParts[1]];
                // set index
                var id = NativeGetPedVariation(componentId, componentWhat);
                item.Index = id;
                // we also gray out any items that have only one option
                var num = NativeGetNumPedVariations(componentId, componentWhat);
                item.Enabled = (num >= 2);
            }
        }
    }

    public void OnItemSelectModel(UIMenu sender, UIMenuItem selectedItem, int index)
    {
        NativeSetPlayerModel(new Model(_pedhash[selectedItem.Text]));
    }

    public void OnListChangeOutfit(UIMenu sender, UIMenuListItem listItem, int newIndex)
    {
        var itemParts = listItem.Text.Split(' ');
        ComponentId componentId = _componentid[itemParts[0]];
        ComponentWhat componentWhat = _componentwhat[itemParts[1]];
        int id = newIndex;
        var currentId = new Dictionary<ComponentWhat, int>
        {
            { ComponentWhat.Drawable, NativeGetPedVariation(componentId, ComponentWhat.Drawable) },
            { ComponentWhat.Texture, NativeGetPedVariation(componentId, ComponentWhat.Texture) },
        };
        // we need to ensure that the new id is valid as the menu has more items than number of ids supported by the game
        // GET_NUMBER_OF_PED_..._VARIATIONS sometimes returns 0, in this case there is also exactly one variation
        var num = Math.Max(0, NativeGetNumPedVariations(componentId, componentWhat) - 1);
        if (id > num)
        {
            // wrap the index depending on whether user scrolled forward or backward
            id = (listItem.Index == UI_LIST_MAX - 1) ? num : 0;
            listItem.Index = id;
        }
        currentId[componentWhat] = id;
        NativeSetComponentVariation((int)componentId, currentId[ComponentWhat.Drawable], currentId[ComponentWhat.Texture]);
    }

    public void OnItemSelectCompvar(UIMenu sender, UIMenuItem selectedItem, int index)
    {
        if (selectedItem.Text == "Set")
        {
            int componentId = ((UIMenuListItem)sender.MenuItems[0]).Index;
            int drawableId = ((UIMenuListItem)sender.MenuItems[1]).Index;
            int textureId = ((UIMenuListItem)sender.MenuItems[2]).Index;
            NativeSetComponentVariation(componentId, drawableId, textureId);
        }
    }

    public  void NativeSetPlayerModel(Model model)
    {
        model.Request(500);
        if (model.IsInCdImage && model.IsValid)
        {
            while (!model.IsLoaded) Script.Wait(100);
            Function.Call(Hash.SET_PLAYER_MODEL, Game.Player, model.Hash);
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character.Handle);
        }
        else
        {
            UI.Notify("could not request model");
        }
        model.MarkAsNoLongerNeeded();
    }

    public int NativeGetNumPedVariations(ComponentId componentId, ComponentWhat componentWhat)
    {
        GTA.Native.Hash hash = (componentWhat == ComponentWhat.Drawable) ? Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS : Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS;
        return Function.Call<int>(hash, Game.Player.Character.Handle, (int)componentId);
    }

    public int NativeGetPedVariation(ComponentId componentId, ComponentWhat componentWhat)
    {
        GTA.Native.Hash hash = (componentWhat == ComponentWhat.Drawable) ? Hash.GET_PED_DRAWABLE_VARIATION : Hash.GET_PED_TEXTURE_VARIATION;
        return Function.Call<int>(hash, Game.Player.Character.Handle, (int)componentId);
    }

    public void NativeSetComponentVariation(int componentId, int drawableId, int textureId)
    {
        Function.Call(
            Hash.SET_PED_COMPONENT_VARIATION,
            Game.Player.Character.Handle,
            componentId,
            drawableId,
            textureId,
            2); // 2 = paletteId
    }
}

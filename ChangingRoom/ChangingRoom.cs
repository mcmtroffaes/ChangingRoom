/*
Changing Room - a GTA V Mod for changing models and outfits
Copyright(C) 2015 Matthias C. M. Troffaes

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License along
with this program; if not, write to the Free Software Foundation, Inc.,
51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

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
    static int UI_LIST_MAX = 100;

    private readonly Dictionary<string, PedHash> _pedhash;
    private readonly Dictionary<string, ComponentId> _componentid;
    private readonly Dictionary<string, ComponentWhat> _componentwhat;

    private MenuPool menuPool;

    public UIMenu AddSubMenu(UIMenu menu, string name)
    {
        var item = new UIMenuItem(name);
        menu.AddItem(item);
        var submenu = new UIMenu(menu.Title.Caption, name);
        menuPool.Add(submenu);
        menu.BindMenuToItem(submenu, item);
        return submenu;
    }

    public void RefreshIndex()
    {
        foreach (UIMenu menu in menuPool.ToList()) menu.RefreshIndex();
    }

    public void AddStorymodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Story Mode");
        AddStorymodeModelToMenu(submenu);
        AddStorymodeOutfitToMenu(submenu);
    }

    public void AddStorymodeModelToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Change Model");
        foreach (var pedItem in ChangingRoomPeds.peds)
            AddCategoryToMenu(submenu, pedItem.Item1, pedItem.Item2);
    }

    public void AddCategoryToMenu(UIMenu menu, string name, PedHash[] models)
    {
        var submenu = AddSubMenu(menu, name);
        foreach (PedHash model in models)
        {
            var subitem = new UIMenuItem(model.ToString());
            submenu.AddItem(subitem);
        }
        submenu.OnItemSelect += (sender, item, index) => NativeSetPlayerModel(_pedhash[item.Text]);
    }

    public void AddStorymodeOutfitToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Change Outfit");
        foreach (ComponentId componentId in Enum.GetValues(typeof(ComponentId)))
            foreach (ComponentWhat componentWhat in Enum.GetValues(typeof(ComponentWhat)))
                AddComponentToMenu(submenu, componentId, componentWhat);
        menu.OnItemSelect += OnItemSelectOutfit;
        submenu.OnListChange += OnListChangeOutfit;
    }

    public void AddComponentToMenu(UIMenu menu, ComponentId componentId, ComponentWhat componentWhat)
    {
        menu.AddItem(new UIMenuListItem(
            componentId.ToString() + " " + componentWhat.ToString(),
            Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList(),
            0));
    }

    public void AddFreemodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Free Mode");
        AddFreemodeModelToMenu(submenu);
        AddFreemodeOutfitToMenu(submenu);
    }

    public void AddFreemodeModelToMenu(UIMenu menu)
    {
        menu.AddItem(new UIMenuItem("Male"));
        menu.AddItem(new UIMenuItem("Female"));
        menu.OnItemSelect += (sender, item, index) =>
        {
            if (item.Text == "Male")
                NativeSetPlayerModel(PedHash.FreemodeMale01);
            else if (item.Text == "Female")
                NativeSetPlayerModel(PedHash.FreemodeFemale01);
        };
    }

    public void AddFreemodeOutfitToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Change Outfit");
        // TODO needs different component ids
        foreach (ComponentId componentId in Enum.GetValues(typeof(ComponentId)))
            foreach (ComponentWhat componentWhat in Enum.GetValues(typeof(ComponentWhat)))
                AddComponentToMenu(submenu, componentId, componentWhat);
        menu.OnItemSelect += OnItemSelectOutfit;
        submenu.OnListChange += OnListChangeOutfit;
    }

    public void AddExpertmodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Expert Mode");
        AddExpertmodeCompvarToMenu(submenu);
        AddExpertmodeComprandomToMenu(submenu);
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
                NativeGetPedDrawableVariation(0)));
        submenu.AddItem(
            new UIMenuListItem(
                "Texture Id",
                Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList(),
                NativeGetPedTextureVariation(0)));
        submenu.AddItem(
            new UIMenuListItem(
                "Palette Id",
                Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList(),
                NativeGetPedPaletteVariation(0)));
        submenu.OnListChange += (sender, item, index) =>
        {
            if (item.Text != "Component Id")
            {
                // changing drawable/texture/palette: update in-game values
                var componentId = ((UIMenuListItem)sender.MenuItems[0]).Index;
                var drawableId = ((UIMenuListItem)sender.MenuItems[1]).Index;
                var textureId = ((UIMenuListItem)sender.MenuItems[2]).Index;
                var paletteId = ((UIMenuListItem)sender.MenuItems[3]).Index;
                NativeSetPedComponentVariation(componentId, drawableId, textureId, paletteId);
            }
            else
            {
                // changing component: sync menu indices with in-game drawable/texture/palette
                var componentId = index;
                ((UIMenuListItem)sender.MenuItems[1]).Index = NativeGetPedDrawableVariation(componentId);
                ((UIMenuListItem)sender.MenuItems[2]).Index = NativeGetPedTextureVariation(componentId);
                ((UIMenuListItem)sender.MenuItems[3]).Index = NativeGetPedPaletteVariation(componentId);
            }
        };
    }

    public void AddExpertmodeComprandomToMenu(UIMenu menu)
    {
        menu.AddItem(new UIMenuItem("SET_PED_RANDOM_COMPONENT_VARIATION"));
        menu.OnItemSelect += (sender, item, index) =>
        {
            // argument seems to have no effect
            if (item.Text == "SET_PED_RANDOM_COMPONENT_VARIATION") NativeSetPedRandomComponentVariation(false);
        };
    }

    public ChangingRoom()
    {
        menuPool = new MenuPool();
        var menuMain = new UIMenu("Changing Room", "Main Menu");
        menuPool.Add(menuMain);
        AddStorymodeToMenu(menuMain);
        AddFreemodeToMenu(menuMain);
        AddExpertmodeToMenu(menuMain);
        RefreshIndex();

        Tick += (sender, e) => menuPool.ProcessMenus();
        KeyUp += (sender, e) =>
        {
            if (e.KeyCode == Keys.F5 && !menuPool.IsAnyMenuOpen())
                menuMain.Visible = !menuMain.Visible;
        };

        _pedhash = new Dictionary<string, PedHash>();
        _componentid = new Dictionary<string, ComponentId>();
        _componentwhat = new Dictionary<string, ComponentWhat>();
        foreach (PedHash x in Enum.GetValues(typeof(PedHash))) _pedhash[x.ToString()] = x;
        foreach (ComponentId x in Enum.GetValues(typeof(ComponentId))) _componentid[x.ToString()] = x;
        foreach (ComponentWhat x in Enum.GetValues(typeof(ComponentWhat))) _componentwhat[x.ToString()] = x;
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
                // we also gray out any items that have only one option
                if (componentWhat == ComponentWhat.Drawable)
                {
                    var drawableId = NativeGetPedDrawableVariation((int)componentId);
                    item.Index = drawableId;
                    var num = NativeGetNumPedDrawableVariations((int)componentId);
                    item.Enabled = (num >= 2);
                }
                else
                {
                    var textureId = NativeGetPedTextureVariation((int)componentId);
                    item.Index = textureId;
                    var drawableId = NativeGetPedDrawableVariation((int)componentId);
                    var num = NativeGetNumPedTextureVariations((int)componentId, drawableId);
                    item.Enabled = (num >= 2);
                }
            }
        }
    }

    public void OnListChangeOutfit(UIMenu sender, UIMenuListItem listItem, int newIndex)
    {
        var itemParts = listItem.Text.Split(' ');
        var componentId = _componentid[itemParts[0]];
        var componentWhat = _componentwhat[itemParts[1]];
        var id = newIndex;
        var drawableId = NativeGetPedDrawableVariation((int)componentId);
        var textureId = NativeGetPedTextureVariation((int)componentId);
        var drawableNum = NativeGetNumPedDrawableVariations((int)componentId);
        var textureNum = NativeGetNumPedTextureVariations((int)componentId, drawableId);
        // we need to ensure that the new id is valid as the menu has more items than number of ids supported by the game
        var num = (componentWhat == ComponentWhat.Drawable) ? drawableNum : textureNum;
        // GET_NUMBER_OF_PED_..._VARIATIONS sometimes returns 0, in this case there is also exactly one variation
        var maxid = Math.Max(0, num - 1);
        if (id > maxid)
        {
            // wrap the index depending on whether user scrolled forward or backward
            id = (listItem.Index == UI_LIST_MAX - 1) ? maxid : 0;
            listItem.Index = id;
        }
        var textureNum2 = textureNum;  // new textureNum after changing drawableId (if changed)
        if (componentWhat == ComponentWhat.Drawable)
        {
            drawableId = id;
            textureNum2 = NativeGetNumPedTextureVariations((int)componentId, drawableId);
            // correct current texture id if it is out of range
            // we pick the nearest integer
            if (textureId >= textureNum2) textureId = textureNum2 - 1;
        }
        else
        {
            textureId = id;
        }
        var paletteId = 2;  // reasonable default
        NativeSetPedComponentVariation((int)componentId, drawableId, textureId, paletteId);
        // when changing drawableId, the texture item might need to be enabled or disabled
        // textureNum depends on both componentId and drawableId and may now have changed
        if (textureNum != textureNum2)
            // TODO is there a better way to get to the next sibling of listItem?
            foreach (UIMenuListItem item in sender.MenuItems)
                if (item.Text == itemParts[0] + " Texture")
                    item.Enabled = (textureNum2 >= 2);
    }

    public  void NativeSetPlayerModel(PedHash hash)
    {
        var model = new Model(hash);
        model.Request(500);
        if (model.IsInCdImage && model.IsValid)
        {
            while (!model.IsLoaded) Script.Wait(100);
            Function.Call(Hash.SET_PLAYER_MODEL, Game.Player, model.Hash);
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character.Handle);
            // pick a better basis for editing the freemode characters
            // until we have a better way of creating valid combinations
            if (hash == PedHash.FreemodeFemale01)
            {
                NativeSetPedComponentVariation(2, 4, 3, 0);
                NativeSetPedComponentVariation(3, 15, 0, 0);
                NativeSetPedComponentVariation(4, 15, 0, 0);
                NativeSetPedComponentVariation(6, 5, 0, 0);
                NativeSetPedComponentVariation(8, 2, 0, 0);
                NativeSetPedComponentVariation(11, 15, 0, 0);
            }
            else if (hash == PedHash.FreemodeMale01)
            {
                NativeSetPedComponentVariation(2, 10, 1, 0);
                NativeSetPedComponentVariation(3, 15, 0, 0);
                NativeSetPedComponentVariation(4, 14, 0, 0);
                NativeSetPedComponentVariation(6, 2, 6, 0);
                NativeSetPedComponentVariation(8, 57, 0, 0);
                NativeSetPedComponentVariation(11, 15, 0, 0);
            }
        }
        else
        {
            UI.Notify("could not request model");
        }
        model.MarkAsNoLongerNeeded();
    }

    public int NativeGetNumPedDrawableVariations(int componentId)
    {
        return Function.Call<int>(
            Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, Game.Player.Character.Handle, (int)componentId);
    }

    public int NativeGetNumPedTextureVariations(int componentId, int drawableId)
    {
        return Function.Call<int>(
            Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, Game.Player.Character.Handle, (int)componentId, drawableId);
    }

    public int NativeGetPedDrawableVariation(int componentId)
    {
        return Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, Game.Player.Character.Handle, (int)componentId);
    }

    public int NativeGetPedTextureVariation(int componentId)
    {
        return Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, Game.Player.Character.Handle, (int)componentId);
    }

    public int NativeGetPedPaletteVariation(int componentId)
    {
        return Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, Game.Player.Character.Handle, (int)componentId);
    }

    public void NativeSetPedComponentVariation(int componentId, int drawableId, int textureId, int paletteId)
    {
        Function.Call(
            Hash.SET_PED_COMPONENT_VARIATION,
            Game.Player.Character.Handle,
            componentId,
            drawableId,
            textureId,
            paletteId);
    }

    public void NativeSetPedRandomComponentVariation(bool toggle)
    {
        Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, Game.Player.Character.Handle, toggle);
    }
}

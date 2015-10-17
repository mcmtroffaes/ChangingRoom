﻿/*
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

public enum FreemodeComponentId
{
    Face,
    Mask,
    Haircut,
    Arms,
    Pants,
    Parachutes,
    Shoes,
    Accessories,
    Shirts1,
    Armour,
    Decals,
    Shirts2,
}

public enum ComponentWhat
{
    Drawable,
    Texture,
}

public class ChangingRoom : Script
{
    static int UI_LIST_MAX = 100;
    static List<dynamic> UI_LIST = Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList();
    private readonly Dictionary<string, PedHash> _pedhash;
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
            componentId.ToString() + " " + componentWhat.ToString(), UI_LIST, 0));
    }

    public void AddFreemodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Free Mode");
        AddFreemodeModelToMenu(submenu);
        AddFreemodeOutfitToMenu(submenu);
    }

    public void AddFreemodeModelToMenu(UIMenu menu)
    {
        var male = new UIMenuItem("Male");
        var female = new UIMenuItem("Female");
        menu.AddItem(male);
        menu.AddItem(female);
        menu.OnItemSelect += (sender, item, index) =>
        {
            if (item == male)
                NativeSetPlayerModel(PedHash.FreemodeMale01);
            else if (item == female)
                NativeSetPlayerModel(PedHash.FreemodeFemale01);
        };
    }

    public void AddFreemodeOutfitToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Change Outfit");
        foreach (FreemodeComponentId componentId in Enum.GetValues(typeof(FreemodeComponentId)))
            foreach (ComponentWhat componentWhat in Enum.GetValues(typeof(ComponentWhat)))
                AddFreemodeComponentToMenu(submenu, componentId, componentWhat);
        menu.OnItemSelect += OnItemSelectOutfit;
        submenu.OnListChange += OnListChangeOutfit;
    }

    public void AddFreemodeComponentToMenu(UIMenu menu, FreemodeComponentId componentId, ComponentWhat componentWhat)
    {
        menu.AddItem(new UIMenuListItem(
            componentId.ToString() + " " + componentWhat.ToString(), UI_LIST, 0));
    }

    public void AddExpertmodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Expert Mode");
        AddExpertmodeCompvarToMenu(submenu);
        AddExpertmodeComprandomToMenu(submenu);
        AddExpertmodePropsToMenu(submenu);
    }

    public void AddExpertmodeCompvarToMenu(UIMenu menu)
    {
        var componentItem = new UIMenuListItem("Component Id", UI_LIST, 0);
        var drawableItem = new UIMenuListItem("Drawable Id", UI_LIST, 0);
        var textureItem = new UIMenuListItem("Texture Id", UI_LIST, 0);
        var paletteItem = new UIMenuListItem("Palette Id", UI_LIST, 0);
        var submenu = AddSubMenu(menu, "Change Component Variation");
        submenu.AddItem(componentItem);
        submenu.AddItem(drawableItem);
        submenu.AddItem(textureItem);
        submenu.AddItem(paletteItem);
        submenu.OnListChange += (sender, item, index) =>
        {
            if (item != componentItem)
            {
                // changing drawable/texture/palette: update in-game values
                var componentId = componentItem.Index;
                var drawableId = drawableItem.Index;
                var textureId = textureItem.Index;
                var paletteId = paletteItem.Index;
                NativeSetPedComponentVariation(componentId, drawableId, textureId, paletteId);
            }
            else
            {
                // changing component: sync menu indices with in-game drawable/texture/palette
                var MAX_COMPONENTS = 11;
                index = (index != UI_LIST_MAX) ? index : MAX_COMPONENTS;
                index = (index <= MAX_COMPONENTS) ? index : 0;
                componentItem.Index = index;
                drawableItem.Index = NativeGetPedDrawableVariation(index);
                textureItem.Index = NativeGetPedTextureVariation(index);
                paletteItem.Index = NativeGetPedPaletteVariation(index);
            }
        };
    }

    public void AddExpertmodePropsToMenu(UIMenu menu)
    {
        // note: we allow value -1 for drawableId, representing cleared prop
        var propItem = new UIMenuListItem("Prop Id", UI_LIST, 0);
        var drawableItem = new UIMenuListItem("Drawable Id", UI_LIST, 0);
        var textureItem = new UIMenuListItem("Texture Id", UI_LIST, 0);
        var submenu = AddSubMenu(menu, "Change Prop Variation");
        submenu.AddItem(propItem);
        submenu.AddItem(drawableItem);
        submenu.AddItem(textureItem);
        submenu.OnListChange += (sender, item, index) =>
        {
            if (item == drawableItem || item == textureItem)
            {
                // changing drawable/texture: update in-game values
                var propId = propItem.Index;
                var drawableMax = NativeGetNumberOfPedPropDrawableVariations(propId) - 1;
                var drawableId = drawableItem.Index - 1;
                if (drawableId > drawableMax)
                {
                    drawableId = (drawableId == UI_LIST_MAX - 2) ? drawableMax : -1;
                    drawableItem.Index = drawableId + 1;
                }
                var textureId = 0;
                if (drawableMax >= 0)
                {
                    var textureMax = Math.Max(0, NativeGetNumberOfPedPropTextureVariations(propId, drawableId) - 1);
                    textureId = textureItem.Index;
                    if (textureId > textureMax)
                    {
                        textureId = (textureId == UI_LIST_MAX - 1) ? textureMax : 0;
                        textureItem.Index = textureId;
                    }
                    textureItem.Enabled = (textureMax >= 1);
                }
                else
                {
                    textureItem.Index = 0;
                    textureItem.Enabled = false;
                }
                if (drawableId >= 0)
                {
                    NativeSetPedPropIndex(propId, drawableId, textureId);
                }
                else
                {
                    NativeClearPedProp(propId);
                }
            }
            else if (item == propItem)
            {
                // changing prop: sync menu indices with in-game drawable/texture
                var propMax = 11;
                var propId = index;
                if (propId > propMax)
                {
                    propId = (propId == UI_LIST_MAX - 1) ? propMax : 0;
                    propItem.Index = propId;
                }
                var drawableId = NativeGetPedPropIndex(propId);
                var textureId = NativeGetPedPropTextureIndex(propId);
                drawableItem.Index = drawableId + 1;
                textureItem.Index = drawableId >= 0 ? textureId : 0;
                drawableItem.Enabled = NativeGetNumberOfPedPropDrawableVariations(propId) >= 1;
                textureItem.Enabled = NativeGetNumberOfPedPropTextureVariations(propId, drawableItem.Index) >= 2;
            }
        };
    }

    public void AddExpertmodeComprandomToMenu(UIMenu menu)
    {
        var randItem = new UIMenuItem("Randomize Component Variation");
        menu.AddItem(randItem);
        menu.OnItemSelect += (sender, item, index) =>
        {
            if (item == randItem) NativeSetPedRandomComponentVariation(false);
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
        foreach (PedHash x in Enum.GetValues(typeof(PedHash))) _pedhash[x.ToString()] = x;
    }

    public void OnItemSelectOutfit(UIMenu sender, UIMenuItem selectedItem, int index)
    {
        if (selectedItem.Text == "Change Outfit")
        {
            // we need to get the ids of every item and set the list indices accordingly
            var itemIndex = 0;
            foreach(UIMenuListItem item in sender.Children[selectedItem].MenuItems)
            {
                var componentId = itemIndex / 2;
                var componentWhat = itemIndex % 2;
                itemIndex++;
                // set index
                // we also gray out any items that have only one option
                if (componentWhat == 0)
                {
                    var drawableId = NativeGetPedDrawableVariation(componentId);
                    item.Index = drawableId;
                    var num = NativeGetNumPedDrawableVariations(componentId);
                    item.Enabled = (num >= 2);
                }
                else
                {
                    var textureId = NativeGetPedTextureVariation(componentId);
                    item.Index = textureId;
                    var drawableId = NativeGetPedDrawableVariation(componentId);
                    var num = NativeGetNumPedTextureVariations(componentId, drawableId);
                    item.Enabled = (num >= 2);
                }
            }
        }
    }

    public void OnListChangeOutfit(UIMenu sender, UIMenuListItem listItem, int newIndex)
    {
        var itemIndex = sender.MenuItems.IndexOf(listItem);
        var componentId = itemIndex / 2;
        var componentWhat = itemIndex % 2;
        var id = newIndex;
        var drawableId = NativeGetPedDrawableVariation(componentId);
        var textureId = NativeGetPedTextureVariation(componentId);
        var drawableNum = NativeGetNumPedDrawableVariations(componentId);
        var textureNum = NativeGetNumPedTextureVariations(componentId, drawableId);
        // we need to ensure that the new id is valid as the menu has more items than number of ids supported by the game
        var num = (componentWhat == 0) ? drawableNum : textureNum;
        // GET_NUMBER_OF_PED_..._VARIATIONS sometimes returns 0, in this case there is also exactly one variation
        var maxid = Math.Max(0, num - 1);
        if (id > maxid)
        {
            // wrap the index depending on whether user scrolled forward or backward
            id = (listItem.Index == UI_LIST_MAX - 1) ? maxid : 0;
            listItem.Index = id;
        }
        var textureNum2 = textureNum;  // new textureNum after changing drawableId (if changed)
        if (componentWhat == 0)
        {
            drawableId = id;
            textureNum2 = NativeGetNumPedTextureVariations(componentId, drawableId);
            // correct current texture id if it is out of range
            // we pick the nearest integer
            if (textureId >= textureNum2) textureId = textureNum2 - 1;
        }
        else
        {
            textureId = id;
        }
        var paletteId = 0;  // reasonable default
        NativeSetPedComponentVariation(componentId, drawableId, textureId, paletteId);
        // when changing drawableId, the texture item might need to be enabled or disabled
        // textureNum depends on both componentId and drawableId and may now have changed
        if (componentWhat == 0 && textureNum != textureNum2)
            sender.MenuItems[itemIndex + 1].Enabled = (textureNum2 >= 2);
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
            Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, Game.Player.Character.Handle, componentId);
    }

    public int NativeGetNumPedTextureVariations(int componentId, int drawableId)
    {
        return Function.Call<int>(
            Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, Game.Player.Character.Handle, componentId, drawableId);
    }

    public int NativeGetPedDrawableVariation(int componentId)
    {
        return Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, Game.Player.Character.Handle, componentId);
    }

    public int NativeGetPedTextureVariation(int componentId)
    {
        return Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, Game.Player.Character.Handle, componentId);
    }

    public int NativeGetPedPaletteVariation(int componentId)
    {
        return Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, Game.Player.Character.Handle, componentId);
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

    public void NativeSetPedPropIndex(int propId, int drawableId, int textureId)
    {
        Function.Call(Hash.SET_PED_PROP_INDEX, Game.Player.Character.Handle, propId, drawableId, textureId);
    }

    public int NativeGetPedPropIndex(int propId) // returns drawableId
    {
        return Function.Call<int>(Hash.GET_PED_PROP_INDEX, Game.Player.Character.Handle, propId);
    }

    public int NativeGetPedPropTextureIndex(int propId) // returns textureId
    {
        return Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, Game.Player.Character.Handle, propId);
    }

    public void NativeClearPedProp(int propId) // returns textureId
    {
        Function.Call(Hash.CLEAR_PED_PROP, Game.Player.Character.Handle, propId);
    }

    public int NativeGetNumberOfPedPropDrawableVariations(int propId)
    {
        return Function.Call<int>(
            Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS, Game.Player.Character.Handle, propId);
    }

    public int NativeGetNumberOfPedPropTextureVariations(int propId, int drawableId)
    {
        return Function.Call<int>(
            Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS, Game.Player.Character.Handle, propId, drawableId);
    }
}

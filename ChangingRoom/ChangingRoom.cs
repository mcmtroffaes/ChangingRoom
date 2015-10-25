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

// all named components, both sp and mp characters
// there are only 11 components for both sp and mp
// the mapping to actual component numbers is defined
// further
public enum Component
{
    Face,
    Beard,
    Haircut,
    Shirt,
    SubShirt,
    Pants,
    Hands,
    Shoes,
    Eyes,
    Mask,
    Armour,
    Parachutes,
    Accessories,
    Items,
    Decals,
    Collars,
}

public class ChangingRoom : Script
{
    static int UI_LIST_MAX = 256;
    static List<dynamic> UI_LIST = Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList();
    private readonly Dictionary<string, PedHash> _pedhash;
    private MenuPool menuPool;
    public enum PlayerType
    {
        PlayerSP,
        PlayerMPMale,
        PlayerMPFemale,
    }
    public PlayerType player_type = PlayerType.PlayerSP;

    public readonly Dictionary<Component, int> sp_componentmap = new Dictionary<Component, int>
    {
        [Component.Face] = 0,
        [Component.Beard] = 1,
        [Component.Haircut] = 2,
        [Component.Shirt] = 3,
        [Component.Pants] = 4,
        [Component.Hands] = 5,
        [Component.Shoes] = 6,
        [Component.Eyes] = 7,
        [Component.Accessories] = 8,
        [Component.Items] = 9,
        [Component.Decals] = 10,
        [Component.Collars] = 11,
    };

    public readonly Dictionary<Component, int> mp_componentmap = new Dictionary<Component, int>
    {
        [Component.Face] = 0,
        [Component.Mask] = 1,
        [Component.Haircut] = 2,
        [Component.Hands] = 3,
        [Component.Pants] = 4,
        [Component.Parachutes] = 5,
        [Component.Shoes] = 6,
        [Component.Accessories] = 7,
        [Component.SubShirt] = 8,
        [Component.Armour] = 9,
        [Component.Decals] = 10,
        [Component.Shirt] = 11,
    };

    // map drawable shirt (component 11) to drawable hand (component 3)
    public readonly Dictionary<int, int> mp_m_shirt_hands = new Dictionary<int, int> {
        [0] = 0,
        [1] = 0,
        [2] = 2,
        [3] = 14,
        [4] = 14,
        [5] = 5,
        [6] = 14,
        [7] = 14,
        [8] = 8,
        [9] = 0,
        [10] = 14,
        [11] = 5,
        [12] = 1,
        [13] = 0,
        [14] = 1,
        [15] = 15,
        [16] = 0,
        [17] = 5,
    };

    public UIMenu AddSubMenu(UIMenu menu, string name)
    {
        return AddSubMenu2(menu, name).Item2;
    }

    public Tuple<UIMenuItem, UIMenu> AddSubMenu2(UIMenu menu, string name)
    {
        var item = new UIMenuItem(name);
        menu.AddItem(item);
        var submenu = new UIMenu(menu.Title.Caption, name);
        menuPool.Add(submenu);
        menu.BindMenuToItem(submenu, item);
        return Tuple.Create(item, submenu);
    }

    public void RefreshIndex()
    {
        foreach (UIMenu menu in menuPool.ToList()) menu.RefreshIndex();
    }

    public void AddStorymodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Story Mode");
        AddStorymodeModelToMenu(submenu);
        AddOutfitToMenu(submenu, sp_componentmap);
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
        submenu.OnItemSelect += (sender, item, index) =>
        {
            NativeSetPlayerModel(_pedhash[item.Text]);
            player_type = PlayerType.PlayerSP;
        };
    }

    public void AddOutfitToMenu(UIMenu menu, Dictionary<Component, int> componentmap)
    {
        var result = AddSubMenu2(menu, "Change Outfit");
        var outfititem = result.Item1;
        var outfitmenu = result.Item2;
        var componentitems = new List<Tuple<Component, UIMenuItem>>();
        foreach (Component component in Enum.GetValues(typeof(Component)))
            if (componentmap.ContainsKey(component))
            {
                var subitem = AddComponentToMenu(outfitmenu, component.ToString(), componentmap[component]);
                componentitems.Add(Tuple.Create(component, subitem));
            }
        menu.OnItemSelect += (sender, item, index) =>
        {
            // enable only if there are any items to change
            if (item == outfititem)
            {
                foreach (var componentitem in componentitems)
                {
                    var component = componentitem.Item1;
                    var subitem = componentitem.Item2;
                    var componentid = componentmap[component];
                    subitem.Enabled = (NativeGetNumPedDrawableVariations(componentid) >= 2) || (NativeGetNumPedTextureVariations(componentid, 0) >= 2);
                }
            }
        };
    }

    public UIMenuItem AddComponentToMenu(UIMenu menu, string text, int componentid)
    {
        var result = AddSubMenu2(menu, text);
        var subitem = result.Item1;
        var submenu = result.Item2;
        var drawableitem = new UIMenuListItem("Model", UI_LIST, 0);
        var textureitem = new UIMenuListItem("Texture", UI_LIST, 0);
        submenu.AddItem(drawableitem);
        submenu.AddItem(textureitem);
        menu.OnItemSelect += (sender, item, index) =>
        {
            if (item == subitem)
            {
                // display correct indices for model and texture
                // and enable item if there's anything to change
                var drawableid = NativeGetPedDrawableVariation(componentid);
                drawableitem.Index = drawableid;
                drawableitem.Enabled = (NativeGetNumPedDrawableVariations(componentid) >= 2);
                var textureid = NativeGetPedTextureVariation(componentid);
                textureitem.Index = textureid;
                textureitem.Enabled = (NativeGetNumPedTextureVariations(componentid, drawableid) >= 2);
            }
        };
        submenu.OnListChange += (sender, item, index) =>
        {
            if (item == drawableitem || item == textureitem)
            {
                var drawableId = NativeGetPedDrawableVariation(componentid);
                var textureId = NativeGetPedTextureVariation(componentid);
                var drawableNum = NativeGetNumPedDrawableVariations(componentid);
                var textureNum = NativeGetNumPedTextureVariations(componentid, drawableId);
                // we need to ensure that the new id is valid as the menu has more items than number of ids supported by the game
                var num = (item == drawableitem) ? drawableNum : textureNum;
                // GET_NUMBER_OF_PED_..._VARIATIONS sometimes returns 0, in this case there is also exactly one variation
                var maxid = Math.Max(0, num - 1);
                if (index > maxid)
                {
                    // wrap the index depending on whether user scrolled forward or backward
                    index = (index == UI_LIST_MAX - 1) ? maxid : 0;
                    item.Index = index;
                }
                var textureNum2 = textureNum;  // new textureNum after changing drawableId (if changed)
                if (item == drawableitem)
                {
                    drawableId = index;
                    textureNum2 = NativeGetNumPedTextureVariations(componentid, drawableId);
                    // correct current texture id if it is out of range
                    // we pick the nearest integer
                    if (textureId >= textureNum2) textureId = textureNum2 - 1;
                }
                else
                {
                    textureId = index;
                }
                NativeSetPedComponentVariation(componentid, drawableId, textureId, -1);
                // when changing drawableId, the texture item might need to be enabled or disabled
                // textureNum depends on both componentId and drawableId and may now have changed
                if (item == drawableitem && textureNum != textureNum2)
                    textureitem.Enabled = (textureNum2 >= 2);
                // try to keep freemode outfit in a valid state
                if (player_type == PlayerType.PlayerMPMale && componentid == 11)
                {
                    if (mp_m_shirt_hands.ContainsKey(drawableId))
                        NativeSetPedComponentVariation(3, mp_m_shirt_hands[componentid], -1, -1);
                }
            }
        };
        return subitem;
    }

    public void AddFreemodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Free Mode");
        AddFreemodeModelToMenu(submenu);
        AddOutfitToMenu(submenu, mp_componentmap);
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
            {
                NativeSetPlayerModel(PedHash.FreemodeMale01);
                player_type = PlayerType.PlayerMPMale;
            }
            else if (item == female)
            {
                NativeSetPlayerModel(PedHash.FreemodeFemale01);
                player_type = PlayerType.PlayerMPFemale;
            }
        };
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

    public void NativeSetPlayerModel(PedHash hash)
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
            (drawableId != -1) ? drawableId : NativeGetPedDrawableVariation(componentId),
            (textureId != -1) ? textureId : NativeGetPedTextureVariation(componentId),
            (paletteId != -1) ? paletteId : NativeGetPedPaletteVariation(componentId));
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

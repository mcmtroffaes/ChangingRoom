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
    // components
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
    // props
    Hats,
    Glasses,
    Earrings,
    Watches,
    Bangles,
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
        [Component.Hats] = 1000,
        [Component.Glasses] = 1001,
        [Component.Earrings] = 1002,
        [Component.Watches] = 1006,
        [Component.Bangles] = 1007,
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
        [Component.Hats] = 1000,
        [Component.Glasses] = 1001,
        [Component.Earrings] = 1002,
        [Component.Watches] = 1006,
        [Component.Bangles] = 1007,
    };

    public readonly List<int> mp_male_clear_drawable = new List<int>
    {
        0, 0, 0, 3, 11, 0, 13, 0, 15, 0, 0, 15
    };

    public readonly List<int> mp_female_clear_drawable = new List<int>
    {
        0, 0, 0, 8, 13, 0, 12, 0, 14, 0, 0, 82
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
        AddChangeOutfitToMenu(submenu, sp_componentmap);
        AddClearOutfitToMenu(submenu);
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

    public void AddChangeOutfitToMenu(UIMenu menu, Dictionary<Component, int> componentmap)
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
                    subitem.Enabled = (GetNumPedDrawableVariations(componentid) >= 2) || (GetNumPedTextureVariations(componentid, 0) >= 2);
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
        var clearitem = new UIMenuItem("Clear");
        submenu.AddItem(drawableitem);
        submenu.AddItem(textureitem);
        submenu.AddItem(clearitem);
        menu.OnItemSelect += (sender, item, index) =>
        {
            if (item == subitem)
            {
                // display correct indices for model and texture
                // and enable item if there's anything to change
                var drawableid = GetPedDrawableVariation(componentid);
                drawableitem.Index = drawableid;
                drawableitem.Enabled = (GetNumPedDrawableVariations(componentid) >= 2);
                var textureid = GetPedTextureVariation(componentid);
                textureitem.Index = textureid;
                textureitem.Enabled = (GetNumPedTextureVariations(componentid, drawableid) >= 2);
            }
        };
        submenu.OnListChange += (sender, item, index) =>
        {
            if (item == drawableitem || item == textureitem)
            {
                var drawableId = GetPedDrawableVariation(componentid);
                var textureId = GetPedTextureVariation(componentid);
                var drawableNum = GetNumPedDrawableVariations(componentid);
                var textureNum = GetNumPedTextureVariations(componentid, drawableId);
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
                    textureNum2 = GetNumPedTextureVariations(componentid, drawableId);
                    // correct current texture id if it is out of range
                    // we pick the nearest integer
                    if (textureId >= textureNum2) textureId = textureNum2 - 1;
                }
                else
                {
                    textureId = index;
                }
                SetPedComponentVariation(componentid, drawableId, textureId, 0);
                // when changing drawableId, the texture item might need to be enabled or disabled
                // textureNum depends on both componentId and drawableId and may now have changed
                if (item == drawableitem && textureNum != textureNum2)
                    textureitem.Enabled = (textureNum2 >= 2);
            }
        };
        submenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == clearitem)
            {
                ClearPedComponentVariation(componentid);
                // update menu items
                var drawableId = GetPedDrawableVariation(componentid);
                var textureId = GetPedTextureVariation(componentid);
                var textureNum = GetNumPedTextureVariations(componentid, drawableId);
                drawableitem.Index = drawableId;
                textureitem.Index = textureId;
                textureitem.Enabled = (textureNum >= 2);
            }
        };
        return subitem;
    }

    public void AddFreemodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Free Mode");
        AddFreemodeModelToMenu(submenu);
        AddChangeOutfitToMenu(submenu, mp_componentmap);
        AddClearOutfitToMenu(submenu);
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

    public void AddClearOutfitToMenu(UIMenu menu)
    {
        var clearitem = new UIMenuItem("Clear Outfit");
        menu.AddItem(clearitem);
        menu.OnItemSelect += (sender, item, index) =>
        {
            if (item == clearitem)
            {
                // components
                for (int componentid = 0; componentid < 12; componentid++)
                    ClearPedComponentVariation(componentid);
                // props
                for (int componentid = 1000; componentid < 1008; componentid++)
                    ClearPedComponentVariation(componentid);
            }
        };
    }

    public void ClearPedComponentVariation(int componentid)
    {
        if (player_type == PlayerType.PlayerMPMale && componentid < 12)
        {
            SetPedComponentVariation(componentid, mp_male_clear_drawable[componentid], 0, 0);
        }
        else if (player_type == PlayerType.PlayerMPFemale && componentid < 12)
        {
            SetPedComponentVariation(componentid, mp_female_clear_drawable[componentid], 0, 0);
        }
        else
        {
            SetPedComponentVariation(componentid, 0, 0, 0);
        }
    }

    public ChangingRoom()
    {
        menuPool = new MenuPool();
        var menuMain = new UIMenu("Changing Room", "Main Menu");
        menuPool.Add(menuMain);
        AddStorymodeToMenu(menuMain);
        AddFreemodeToMenu(menuMain);
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
            componentId, drawableId, textureId, paletteId);
    }

    public void NativeSetPedRandomComponentVariation(bool toggle)
    {
        Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, Game.Player.Character.Handle, toggle);
    }

    public void NativeSetPedPropIndex(int propId, int drawableId, int textureId)
    {
        Function.Call(Hash.SET_PED_PROP_INDEX, Game.Player.Character.Handle, propId, drawableId, textureId, true);
    }

    public int NativeGetPedPropIndex(int propId) // returns drawableId
    {
        return Function.Call<int>(Hash.GET_PED_PROP_INDEX, Game.Player.Character.Handle, propId);
    }

    public int NativeGetPedPropTextureIndex(int propId) // returns textureId
    {
        return Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, Game.Player.Character.Handle, propId);
    }

    public void NativeClearPedProp(int propId)
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

    public int GetNumPedDrawableVariations(int componentId)
    {
        if (componentId < 1000)
            return NativeGetNumPedDrawableVariations(componentId);
        else
            // no prop = extra drawable variation in this script
            return NativeGetNumberOfPedPropDrawableVariations(componentId - 1000) + 1;
    }

    public int GetNumPedTextureVariations(int componentId, int drawableId)
    {
        if (componentId < 1000)
        {
            return NativeGetNumPedTextureVariations(componentId, drawableId);
        }
        else
        {
            if (drawableId == 0)
                // no prop, so no textures
                return 0;
            else
                // native drawableId = script drawableId - 1
                // sometimes returns -1 if there are no textures, so max with 0
                return Math.Max(0, NativeGetNumberOfPedPropTextureVariations(componentId - 1000, drawableId - 1));
        }
    }

    public int GetPedDrawableVariation(int componentId)
    {
        if (componentId < 1000)
            return NativeGetPedDrawableVariation(componentId);
        else
            // script drawableId = native drawableId + 1
            return NativeGetPedPropIndex(componentId - 1000) + 1;
    }

    public int GetPedTextureVariation(int componentId)
    {
        if (componentId < 1000)
            return NativeGetPedTextureVariation(componentId);
        else
            // can return negative index; this means that there is no choice so just return 0
            return Math.Max(0, NativeGetPedPropTextureIndex(componentId - 1000));
    }

    public void SetPedComponentVariation(int componentId, int drawableId, int textureId, int paletteId)
    {
        if (componentId < 1000)
        {
            NativeSetPedComponentVariation(componentId, drawableId, textureId, paletteId);
        }
        else
        {
            if (drawableId == 0)
                // no prop
                NativeClearPedProp(componentId - 1000);
            else
                // native drawableId = script drawableId - 1
                NativeSetPedPropIndex(componentId - 1000, drawableId - 1, textureId);
        }
    }
}

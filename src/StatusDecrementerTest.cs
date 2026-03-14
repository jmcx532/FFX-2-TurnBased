// SPDX-License-Identifier: MIT
/*
 * Not currently using this module.
 * A test module for certain status handling
 * Mainly for 'timed' statuses, Shell, Poison/Regen, Doom etc.
 * 
 * 
 */
using TerraFX.Interop.Windows;

namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public unsafe class StatusDecrementerModule : FhModule {

    //protected readonly FhLogger _logger;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void status_time_decrementer(uint chr_id, int param_2, int param_3, int param_4);
    private readonly FhMethodHandle<status_time_decrementer>_status_time_handler;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate nint get_chr_addr(uint chr_id);
    private readonly FhMethodHandle<get_chr_addr>_get_chr_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    //sub-function that checks for Stop/Petrify/Sleep
    public delegate uint status_fx(uint chr_id, int param_2);
    private readonly FhMethodHandle<status_fx> _status_fx_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int* stone_fx(int param_1, int* param_2);
    private readonly FhMethodHandle<stone_fx> _stone_fx_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint death_fx(uint chr_id, nint chr_base, int cmd_id, int param_4);
    private readonly FhMethodHandle<death_fx> _death_fx_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint eject_fx(uint chr_id, nint chr_base, int cmd_id);
    private readonly FhMethodHandle<eject_fx> _eject_fx_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int alpha_fx(uint chr_id, int param_2);
    private readonly FhMethodHandle<alpha_fx> _alpha_fx_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint beta_fx(int param_1);
    private readonly FhMethodHandle<beta_fx> _beta_fx_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint charlie_fx(uint chr_id, int param_2);
    private readonly FhMethodHandle<charlie_fx> _charlie_fx_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void delta_fx(uint chr_id);
    private readonly FhMethodHandle<delta_fx> _delta_fx_handle;

    public StatusDecrementerModule() {
        //int addr_offset = 0x400000;

        //_logger = new FhLogger($"{FhUtil.get_timestamp_string()}_TurnBased.log");
        _status_time_handler = new FhMethodHandle<status_time_decrementer>(this, "FFX-2.exe", 0x236ca0, h_status_time_handler);
        _get_chr_handle = new FhMethodHandle<get_chr_addr>(this, "FFX-2.exe", 0x211450, h_get_chr_addr);
        _status_fx_handle = new FhMethodHandle<status_fx>(this, "FFX-2.exe", 0x2430f0, h_status_fx);

        _stone_fx_handle = new FhMethodHandle<stone_fx>(this, "FFX-2.exe", 0x21b5d0, h_stone_fx);
        _death_fx_handle = new FhMethodHandle<death_fx>(this, "FFX-2.exe", 0x216d50, h_death_fx);
        _eject_fx_handle = new FhMethodHandle<eject_fx>(this, "FFX-2.exe", 0x216dc0, h_eject_fx);

        _alpha_fx_handle = new FhMethodHandle<alpha_fx>(this, "FFX-2.exe", 0x21add0, h_alpha_fx);
        _beta_fx_handle = new FhMethodHandle<beta_fx>(this, "FFX-2.exe", 0x21e290, h_beta_fx);
        _charlie_fx_handle = new FhMethodHandle<charlie_fx>(this, "FFX-2.exe", 0x2366e0, h_charlie_fx);
        _delta_fx_handle = new FhMethodHandle<delta_fx>(this, "FFX-2.exe", 0x236590, h_delta_fx);
    }

    //sub-functions
    public nint h_get_chr_addr(uint chr_id) {
        return _get_chr_handle.orig_fptr.Invoke(chr_id);
    }

    public uint h_status_fx(uint chr_id, int param_2) {
        return _status_fx_handle.orig_fptr.Invoke(chr_id, param_2);
    }

    public int* h_stone_fx(int param_1, int* chr_base) {
        return _stone_fx_handle.orig_fptr.Invoke(param_1, chr_base);
    }

    public uint h_death_fx(uint chr_id, nint chr_base, int cmd_id, int param_4) {
        return _death_fx_handle.orig_fptr.Invoke(chr_id, chr_base, cmd_id, param_4);
    }
    public uint h_eject_fx(uint chr_id, nint chr_base, int cmd_id) {
        return _eject_fx_handle.orig_fptr.Invoke(chr_id, chr_base, cmd_id);
    }

    public int h_alpha_fx(uint chr_id, int param_2) {
        return _alpha_fx_handle.orig_fptr.Invoke(chr_id, param_2);
    }

    public uint h_beta_fx(int param_1) {
        return _beta_fx_handle.orig_fptr.Invoke(param_1);
    }

    public uint h_charlie_fx(uint chr_id, int param_2) {
        return _charlie_fx_handle.orig_fptr.Invoke(chr_id, param_2);
    }

    public void h_delta_fx(uint chr_id) {
        _delta_fx_handle.orig_fptr.Invoke(chr_id);
    }

    /*Main function - Status handler---------------------------------------------------------------------------*/
    public void h_status_time_handler(uint chr_id, int cmd_id, int param_3, int param_4) {

        byte cVar1;
        uint uVar2;
        byte cVar3;
        nint chr_base;//iVar4
        uint uVar5;
        int iVar6;
        uint uvar6;
        int iVar7;
        uint *puVar8;
        uint uVar9;
        int iVar10;
        byte *pcVar11;

        chr_base = h_get_chr_addr(chr_id);
        
        uVar2 = *(uint*)(chr_base + 0x434);//gets current status bitfield
        cVar1 = *(byte*)(chr_base + 0x43b);//gets regen time left
        uVar5 = h_status_fx(chr_id, 0); //returns a value where it is not 0 if chr is sleeping, petrified or stopped
        uVar9 = *(uint*)(chr_base + 0x570) | *(uint*)(chr_base + 0x550) | *(uint*)(chr_base + 0x4fc) |
                *(uint*)(chr_base + 0x450);
        *(uint*)(chr_base + 0x434) = uVar9; //Chr status bitfield updated with above result

        /*Here begins the status time updater loop -- note to self and others - need to implement this for statuses to works
        pcVar11 = (byte*)(chr_base + 0x500);
        iVar10 = 0x18;
        do {
            uVar6 = FUN_00624d90((int)pcVar11[-0x4c], (int)*pcVar11, 0xffffff83, 0x7d);
            uVar6 = FUN_00624d90(uVar6, (int)pcVar11[0x74], 0xffffff83, 0x7d);
            cVar3 = FUN_00624d90(uVar6, (int)pcVar11[0x54], 0xffffff83, 0x7d);
            pcVar11[-200] = cVar3;
            pcVar11 = pcVar11 + 1;
            iVar10 = iVar10 + -1;
        } while (iVar10 != 0);
        end of status updater*/

        //if character not stopped
        if (*(byte*)(chr_base + 0x43e) == 0) {
            //if they are hasted
            if (*(byte*)(chr_base + 0x43c) != 0) {
                //Removes slow -set it's timer to 0
                *(byte*)(chr_base + 0x43d) = 0;
            }
        }
        else {
            //if they are stopped - zero there haste timer
            *(byte*)(chr_base + 0x43c) = 0;
        }
        //Petrification
        if ((uVar2 >> 1 & 1) != (uVar9 >> 1 & 1)) {
            h_stone_fx((int)chr_id, (int*)chr_base);
        }
        //Instant death effects
        if ((uVar2 & 1) != (uVar9 & 1)) {
            h_death_fx(chr_id, chr_base, cmd_id, param_4);
        }
        //Eject
        if ((uVar2 >> 10 & 1) != (uVar9 >> 10 & 1)) {
            h_eject_fx(chr_id, chr_base, cmd_id);
        }
        //Poison check
        if (((uVar2 >> 5 & 1) == 0) && ((uVar9 & 0x20) != 0)) {
            iVar10 = *(int*)(chr_base + 0x68c);//Poison Time threshold
            iVar6 = h_alpha_fx(chr_id, 0);
            iVar7 = (int)h_beta_fx(iVar6);
            //Poison check accumulator =                //(iVar10 / 4) + 1???
            *(int*)(chr_base + 0x684) = iVar7 % (((int)((iVar10 >> 0x1f & 3U) + iVar10) >> 2) + 1);
        }
        //Regen check
        if ((cVar1 == 0) && (*(char*)(chr_base + 0x43b) != '\0')) {
            iVar10 = *(int*)(chr_base + 0x690);//Regen Time Threshols
            iVar6 = h_alpha_fx(chr_id, 0);
            iVar7 = (int)h_beta_fx(iVar6);
            //Regen check accumulator                                //(iVar10 / 4) + 1???
            *(int*)(chr_base + 0x688) = iVar7 % (((int)((iVar10 >> 0x1f & 3U) + iVar10) >> 2) + 1);
        }

        //some other loop
        iVar10 = 0;
        puVar8 = (uint*)(chr_base + 0x454);
        do {
            if (((uVar2 >> ((byte)iVar10 & 0x1f) & 1) == 0) && ((uVar9 >> ((byte)iVar10 & 0x1f) & 1) != 0)) {
                uint DAT_00DF8ea8 = FhUtil.get_at<uint>(0x9f8ea8);
                *puVar8 = (&DAT_00DF8ea8)[iVar10];
            }
            iVar10 = iVar10 + 1;
            puVar8 = puVar8 + 1;
        } while (iVar10 < 0x18);

        uint sps_bitfield; //field where bits are set to 1 if character is asleep, petrified, stopped
        sps_bitfield = h_status_fx(chr_id, 0);
        if ((param_3 != 0) && (uVar5 != sps_bitfield)) {
            h_charlie_fx(chr_id, 3);
        }
        h_delta_fx(chr_id);
        return;
    }
 


    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        /*
        _status_time_handler.hook();
        _get_chr_handle.hook();
        _status_fx_handle.hook();
        _stone_fx_handle.hook();
        _death_fx_handle.hook();
        _eject_fx_handle.hook();
        _alpha_fx_handle.hook();
        _beta_fx_handle.hook();
        _charlie_fx_handle.hook();
        _delta_fx_handle.hook();
        */
        return true;
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}

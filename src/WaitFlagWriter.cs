// SPDX-License-Identifier: MIT


using TerraFX.Interop.Windows;

namespace Fahrenheit.Modules.FFX2TurnBased;

//function delegates
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate uint wait_flag_writer();
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int get_chr_addr(uint chr_id);

[FhLoad(FhGameId.FFX2)]
public class TurnBasedModule : FhModule {
    protected readonly FhLogger _wait_flag_logger;
    private readonly FhMethodHandle<wait_flag_writer>_wait_handler;
    private readonly FhMethodHandle<get_chr_addr> _get_chr_addr;

    public TurnBasedModule() {
        int addr_offset = 0x400000;

        _wait_flag_logger = new FhLogger("WaitFlag_Writer_TurnBased.log");
        _wait_handler = new FhMethodHandle<wait_flag_writer>(this, "FFX-2.exe", 0x634ae0 - addr_offset, h_wait_handler);
        _get_chr_addr = new FhMethodHandle<get_chr_addr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_get_chr_addr);
    }

    /*this function gets the base address of a characters Battle data section
     * If it's parameter is less than 31 it returns a characters BattleData base address
     *If it's passed with a parameter greater than 155 it does end of battle cleanup I noticed from logging before
     * Chr ids: Y: 0, R: 2, P: 3 -- enemies from 15 onward
     */
    public int h_get_chr_addr(uint chr_id) {
        return _get_chr_addr.orig_fptr.Invoke(chr_id);
    }

    
    public unsafe uint h_wait_handler() {

        //wait loop
        for (uint i = 0; i < 31; i++) {
            nint chr_base = h_get_chr_addr(i);
            byte is_active = *(byte*)(chr_base + 0x1784);
            byte state = *(byte*)(chr_base + 0xe68);
            byte num_targets_hit = *(byte*)(chr_base + 0xec2);

            //Are they KOed
            uint status = *(uint*)(chr_base + 0x434);
            bool isAlive = (status & 0x1) == 0;
            if (!isAlive)
                continue; // dead - stop

            //Attack / counter attack handling
            //for counter-attack handling, I had to monitor a certain flag, but it misbehaves if certain commands are used
            // and the wait flag is stuck because it isn't set correctly. This relates to the yrp_state checks below*
            //check for Escape, Scan, Teleport... ,dresspheres are for when SpecialDressphere dies
            var exceptionCommands = new HashSet<int> {0x3001,0x303D, 0x31EE,0x5001,0x5002,0x5003,0x5004,0x5005,0x5006,0x5007,0x5008,
                                                0x5009,0x500A,0x500B,0x500C,0x500D,0x500E,0x500F,0x5010,0x5011,0x5012,
                                                0x5013, 0x5014, 0x5015, 0x5016, 0x5017, 0x5018, 0x5019,0x501A,0x501B, 0x501C, 0x501D,
                                                0x501E, 0x501F, 0x5020, 0x5021};

            //get YRP (some state flag?) and the last command they performed- used for counterattack handling
            int y_addr = h_get_chr_addr(0);
            byte y_tgts_hit = *(byte*)(y_addr + 0xec2);
            ushort y_last_command = *(ushort*)(y_addr + 0xf3c);

            int r_addr = h_get_chr_addr(1);
            byte r_tgts_hit = *(byte*)(r_addr + 0xec2);
            ushort r_last_command = *(ushort*)(r_addr + 0xf3c);

            int p_addr = h_get_chr_addr(2);
            byte p_tgts_hit = *(byte*)(p_addr + 0xec2);
            ushort p_last_command = *(ushort*)(p_addr + 0xf3c);

            if (
            (y_tgts_hit == 0 && isAlive && !exceptionCommands.Contains(y_last_command)) ||
            (r_tgts_hit == 0 && isAlive && !exceptionCommands.Contains(r_last_command)) ||
            (p_tgts_hit == 0 && isAlive && !exceptionCommands.Contains(p_last_command))
        ) {
                //write submenu open wait flag (DAT_00DF8817)
                FhUtil.set_at<byte>(0x9F8817, 1);
                return 1;
            }


            /*DEBUG - chr + 0xe68 State2 is perhaps a brief pause state? Causes issues in Turn-Based 
            if (*(byte*)(chr_base + 0xe68) == 2) {
                FhUtil.set_at<byte>(0x9F8817, 1);
                return 1;
            }*/

            /*debug logging
            if (i == 0xf) {
                int e1_base = h_get_chr_addr(15);
                byte e1_state = *(byte*)(e1_base + 0xe68);
                //_wait_flag_logger.Info("Enemy 1 state is: " + e1_state);
            }*/

        }//end of loop


        //number of allies ready variable/flag (DAT_011B7480)
        int num_allies_ready = FhUtil.get_at<byte>((nint)0xDB7480);

        if (num_allies_ready != 0) {
            FhUtil.set_at<byte>(0x9F8817, 1);
            return 1;
        }
        
        //number of characters acting at 0xDF7903
        int num_characters_acting = FhUtil.get_at<byte>((nint)0x9F7903);
        if (num_characters_acting != 0) {
            FhUtil.set_at<byte>(0x9F8817, 1);
            return 1;
        }

        /*dont wait loop
        for (uint i = 0; i < 31; i++) {
            nint chr_base = h_get_chr_addr(i);
            byte is_active = *(byte*)(chr_base + 0x1784);
            byte state = *(byte*)(chr_base + 0xe68);
            int atb_progress = *(byte*)(chr_base + 0x9d8);

            if (state != 4 && state != 9 && is_active == 1 && atb_progress != 0) {
                FhUtil.set_at<byte>(0x9F8817, 0);
                return 0;
            }

        }*/

        //unset wait flag
        FhUtil.set_at<byte>(0x9F8817, 0);
        return 1;
    }

    //Version 2
    /*
    public unsafe uint h_wait_handler() {

        int num_allies_ready = FhUtil.get_at<byte>((nint)0xDB7480);
        if (num_allies_ready > 0) {
            FhUtil.set_at<byte>(0x9F8817, 1);
            return 1;
        }


        for (uint i = 0; i < 31; i++) {
            nint chr_base = h_get_chr_addr(i);

            //are they an active character - not a weapon or something else.
            if (*(byte*)(chr_base + 0x1784) == 0)
                continue;

            //Are they KOed
            uint status = *(uint*)(chr_base + 0x434);
            bool isAlive = (status & 0x1) == 0;
            if (!isAlive)
                continue; // dead

            int atb_progress = *(int*)(chr_base + 0x9d8);
            int num_characters_acting = FhUtil.get_at<byte>((nint)0x9F7903);
            if (atb_progress < 1 && num_characters_acting > 0) {
                FhUtil.set_at<byte>(0x9F8817, 1);
                return 1;
            }
        }

        //get YRP (some state flag?) and the last command they performed- used for counterattack handling
        int y_addr = h_get_chr_addr(0);
        byte y_state = *(byte*)(y_addr + 0xec2);
        ushort y_last_command = *(ushort*)(y_addr + 0xf3c);

        int r_addr = h_get_chr_addr(1);
        byte r_state = *(byte*)(r_addr + 0xec2);
        ushort r_last_command = *(ushort*)(r_addr + 0xf3c);

        int p_addr = h_get_chr_addr(2);
        byte p_state = *(byte*)(p_addr + 0xec2);
        ushort p_last_command = *(ushort*)(p_addr + 0xf3c);

         //for counter-attack handling, I had to monitor a certain flag, but it misbehaves if certain commands are used
         // and the wait flag is stuck because it isn't set correctly. This relates to the yrp_state checks below*
        //check for Escape, Scan, Teleport... ,dresspheres are for when SpecialDressphere dies
        var exceptionCommands = new HashSet<int> {0x3001,0x303D, 0x31EE,0x5001,0x5002,0x5003,0x5004,0x5005,0x5006,0x5007,0x5008,
                                                0x5009,0x500A,0x500B,0x500C,0x500D,0x500E,0x500F,0x5010,0x5011,0x5012,
                                                0x5013, 0x5014, 0x5015, 0x5016, 0x5017, 0x5018, 0x5019,0x501A,0x501B, 0x501C, 0x501D,
                                                0x501E, 0x501F, 0x5020, 0x5021};

        if (
            (y_state == 0 && !exceptionCommands.Contains(y_last_command)) ||
            (r_state == 0 && !exceptionCommands.Contains(r_last_command)) ||
            (p_state == 0 && !exceptionCommands.Contains(p_last_command))
        ) {
            //write submenu open wait flag (DAT_00DF8817)
            FhUtil.set_at<byte>(0x9F8817, 1);
            return 1;
        }

        FhUtil.set_at<byte>(0x9f8817, 0);
        return 0;
    }
    */

    //version 1-------------------------------------------------------------------------
    /*replaces the function that checks whether a submenu is open and
     * writes the submenu open wait flag
    //runs every frame
    public unsafe uint h_wait_handler() {
        //read and store how many allies have ATB full
        //number of allies ready variable/flag (DAT_011B7480)
        int num_allies_ready = FhUtil.get_at<byte>((nint)0xDB7480);

        //number of enemies alive at 0x11b72dc
        //int enemies_alive = FhUtil.get_at<byte>((nint)0xDB72DC);

        //number of characters acting at 0xDF7903
        int num_characters_acting = FhUtil.get_at<byte>((nint)0x9F7903);


        //get YRP (some state flag?) and the last command they performed- used for counterattack handling
        int y_addr = h_get_chr_addr(0);
        byte y_state = *(byte*)(y_addr + 0xec2);
        ushort y_last_command = *(ushort*)(y_addr + 0xf3c);

        int r_addr = h_get_chr_addr(1);
        byte r_state = *(byte*)(r_addr + 0xec2);
        ushort r_last_command = *(ushort*)(r_addr + 0xf3c);

        int p_addr = h_get_chr_addr(2);
        byte p_state = *(byte*)(p_addr + 0xec2);
        ushort p_last_command = *(ushort*)(p_addr + 0xf3c);

        //if any ally has a full ATB, or a character is acting set the wait flag to 1 (Activate Wait mode)
        //If their state as obtained above is 0 - activate wait mode
        //Function runs every frame in battle
        //THIS WILL ONLY WORK IF COMMANDS HAVE THE CHARGE TIME MECHANIC REMOVED COMPLETELY (Enemies like flans/elementals mess with this)
        
        /*for counter-attack handling, I had to monitor a certain flag, but it misbehaves if certain commands are used
         * and the wait flag is stuck because it isn't set correctly. This relates to the yrp_state checks below*
        //check for Escape, Scan, Teleport... ,dresspheres are for when SpecialDressphere dies
        var exceptionCommands = new HashSet<int> {0x3001,0x303D, 0x31EE,0x5001,0x5002,0x5003,0x5004,0x5005,0x5006,0x5007,0x5008,
                                                0x5009,0x500A,0x500B,0x500C,0x500D,0x500E,0x500F,0x5010,0x5011,0x5012,
                                                0x5013, 0x5014, 0x5015, 0x5016, 0x5017, 0x5018, 0x5019,0x501A,0x501B };

        if ( num_allies_ready != 0 || num_characters_acting != 0) {
            //write submenu open wait flag (DAT_00DF8817)
            FhUtil.set_at<byte>(0x9F8817, 1);

        }
        else if (
            (y_state == 0 && !exceptionCommands.Contains(y_last_command)) ||
            (r_state == 0 && !exceptionCommands.Contains(r_last_command)) ||
            (p_state == 0 && !exceptionCommands.Contains(p_last_command))
        ) {
            //write submenu open wait flag (DAT_00DF8817)
            FhUtil.set_at<byte>(0x9F8817, 1);
        }
        else {
            //otherwise write 0 (Don't activate Wait Mode)
            //submenu open wait flag (DAT_00DF8817)
            FhUtil.set_at<byte>(0x9F8817, 0);
        }

            return 1;
    }
*/

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _wait_handler.hook();
        _get_chr_addr.hook();

        
        return true;
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}

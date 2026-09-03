// SPDX-License-Identifier: MIT
/* A module for FFX-2 that changes the ATB Recovery time after a command has been used
 * Haste halves recovery time while Slow doubles is (similar to FFX)
 * It also reimplements the cooldown reduction from auto-abilities like Black Magic Lv.2 or Turbo Bushido
 * as the Charge Time mechanic was removed.
 * 
 * Pairs up with ATBRecovery_StatusHandling.cs (partial class) which re-implements status duration handling
 * Pairs up with ATBRecovery_TurnList which implements the turn order window
 */

namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public unsafe partial class ATBRecoveryModule : FhModule {

    const ushort SPHERECHANGE_ATB_COST = 25;
    /*
    //function delegates
    //634140 - MsATBgetRestTime
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsATBgetRestTime(byte chr_id, uint command_id);
    private static FhMethodHandle<MsATBgetRestTime> _MsATBgetRestTime =>
        new ( new FhMethodLocation("FFX-2.exe", 0x234140) );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    //6401c0 - MsCommandComplete
    public delegate uint MsCommandComplete(byte chr_id, int param_2, int param_3);
    private static FhMethodHandle<MsCommandComplete> _MsCommandComplete =>
        new ( new FhMethodLocation("FFX-2.exe", 0x2401C0) );

    //611450 - MsGetChr
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetChr(uint chr_id);
    private static FhMethodHandle<MsGetChr> _MsGetChr =>
        new ( new FhMethodLocation("FFX-2.exe", 0x211450) );

    

    //6341a0
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsATBgetThinkingTime(uint chr_id);
    private static FhMethodHandle<MsATBgetThinkingTime> _MsATBgetThinkingTime =>
        new ( new FhMethodLocation("FFX-2.exe", 0x2341A0) );

    //756590 - TOBtlDrawATBGaude - NOT a typo
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void TOBtlDrawATBGaude(int param_1, int param_2, int param_3);
    private static FhMethodHandle<TOBtlDrawATBGaude> _TOBtlDrawATBGaude =>
        new ( new FhMethodLocation("FFX-2.exe", 0x356590) );

    */

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetComData(uint arg1, byte* arg2);
    private static FhMethodHandle<MsGetComData> _MsGetComData =>
        new(new FhMethodLocation("FFX-2.exe", 0x225160));

    public ATBRecoveryModule() { }

    //SUB-FUNCTIONS
    public Chr* h_MsGetChr(uint chr_id) {
        return FFX2.FhCall.MsGetChr.chain_from(h_MsGetChr).fnptr!(chr_id);
    }

    //this function returns the base address for various Excel data types
    //param_1 is the command id (e.g 0x3002)
    public unsafe int h_MsGetComData(uint command_id, byte* param_2) {
        int result = _MsGetComData.chain_from(h_MsGetComData).fnptr!(command_id, param_2);
        return result;
    }
    public int h_MsCheckRange(int param_1, int param_2, int param_3) {
        return FhCall.MsCheckRange.chain_from(h_MsCheckRange).fnptr!(param_1, param_2, param_3);
    }
    //remove thinking time, used to cause a bug with poison/regen, probably OK now, but don't need this mechanic
    public int h_MsATBgetThinkingTime(uint chr_id) {
        int original_result = FFX2.FhCall.MsATBgetThinkingTime.chain_from(h_MsATBgetThinkingTime).fnptr!(chr_id);
        //return 0 instead
        return 0;
    }

    /*
    // Stub out to prevent ATB Gauges from being drawn
    public void h_TOBtlDrawATBGaude(int param_1, int param_2, int param_3) {
        return;
         //FFX2.FhCall.TOBtlDrawATBGaude.chain_from(h_TOBtlDrawATBGaude).fnptr!(param_1, param_2, param_3); // stub out to stop ATB gauges being drawn, or is a mkp function the actual drawer?
    }*/

    //MAIN FUNCTIONS---------------------------------------------------------------------------------------------------
    //called constantly - not a one and done function - use MsCommandComplete for those types of effects
    public uint h_MsATBgetRestTime(byte chr_id, uint command_id) {
        int chr_base_address;
        int cmd_base_address;

        /* Gets the character's base address */
        Chr* chr = h_MsGetChr(chr_id);
        chr_base_address = (int)chr;

        // FUN_00625160 - Get the commands base address, this function can also return other Excel data types
        cmd_base_address = h_MsGetComData(command_id, (byte*)(0));

        //normal calculation
        //read the commands atb_cost and multiply
        int cmd_recovery_time = (int)(*(ushort*)(cmd_base_address + 0x22) * 10000);

        //divide that by (user's Agility + 1) -- VANILLA
        //uint agility_divisor = (uint)(*(byte*)(chr_base_address + 0x39a)) + 1;

        //CUSTOM DIVISOR
        byte agility = (*(byte*)(chr_base_address + 0x39a));
        double divisor;
        //if agility is over 100, use a stronger taper that means higher agility stats don't reduce recovery time as much.
        if (agility > 100) {
            divisor = Math.Pow(agility + 125, 0.85) + 1;
        }
        else {
            //if agility is 99 or less, use vanilla divisor
            divisor = agility + 1;
        }

        uint agility_divisor = (uint)Math.Round(divisor);

        

        //delay from attacks to be added
        uint accrued_delay = (uint)*(int*)(chr_base_address + 0x9e0);
        //calculate ATB timer length and clamp between 0 and 99999
        uint calced_recovery = (uint)h_MsCheckRange((int)((cmd_recovery_time / agility_divisor) + accrued_delay), 0, 99999);


        //Haste / Slow Modifier
        //if character is hasted - half recovery time
        if (*(byte*)(chr_base_address + 0x43c) != '\0')
        {
            calced_recovery = (calced_recovery * 75) / 100;
        }
        //if character is slowed - double recovery time
        if (*(byte*)(chr_base_address + 0x43d) != '\0')
        {
            calced_recovery = (calced_recovery * 125) / 100;
        }


        //auto ability recovery time reduction
        ushort command_used = (*(ushort*)(chr_base_address + 0xf3c));
        int percent_reduction = calc_aa_cmd_recov_reduction(chr_base_address, command_used, (int)cmd_base_address);

        //apply auto ability reduction
        calced_recovery = (uint)((100 - percent_reduction) * calced_recovery) / 100;
            
        // Accrued delay is reset 
        *(int*)(chr_base_address + 0x9e0) = 0;


        //return calculated recovery time to be written
        return calced_recovery;
    }


    // A-Ability Charge time reduction function replacement ------------------------------------------- 
    //returns the proper percentage reduction for auto-abilities that originally reduced charge time
    //so it can be used to reduce ATB recovery instead.
    //For command_used/param_2 it is the command id (for example: 0x308f for Unhinge)
    public int calc_aa_cmd_recov_reduction(int chr_base_addr, uint command_used, int cmd_base_addr) {
        //int calced_charge_time;
        int menu_cmd_addr;
        int recov_time_reduction;
        ushort *puVar1;
        int percent_reduction;
        int incV1;
        
        //if user has no auto-abilities that reduce recovery time (originally charge time) (e.g Turbo Arcana) 
        if (*(char*)(chr_base_addr + 0x5af) == '\0') {
            return 0;
        }

        //initialise incrementer variable 1 and recov_time_reduction
        recov_time_reduction = 0;
        incV1 = 0;

        /*This is a pointer to an array inside the Chr BattleData
        *This contains up to 4 command ids relating to sub-menus (Arcana, Instinct, Black Magic for example)
        *for each auto-ability a character has that reduces a command styles charge time
        */
        puVar1 = (ushort*)(chr_base_addr + 0x5b0);

        do {
            //get the percentage reduction 
            percent_reduction = (int)*(byte*)(chr_base_addr + 0x5b8 + incV1);
            
            //get the base address of the menu command (e.g White Magic, Swordplay etc.)
            menu_cmd_addr = h_MsGetComData((uint)*puVar1, (byte*)(0));
            
                
            //condition 1: do something with the menu commands 'sub_command' parameter
            //condition 2: do something with the player chosen command 'sub_command parameter'
            //condition 3: check if the player chosen commands 'flow_system' parameter matches the menu_cmd 'system' parameter
            if ((((*(byte*)(menu_cmd_addr + 0xd) & 0xf8) != 0) &&
                    ((*(byte*)(cmd_base_addr + 0xd) & 7) == 0)) &&
                   (*(byte*)(cmd_base_addr + 0xf) == *(byte*)(menu_cmd_addr + 0xe))) {
                    recov_time_reduction = recov_time_reduction + percent_reduction;
            }
            
            incV1 = incV1 + 1;
            puVar1 = puVar1 + 1;
        } while (incV1 < (int)(uint)*(byte*)(chr_base_addr + 0x5af));

        //if recovery time is 0, return 0 otherwise return the result
        if (recov_time_reduction == 0) {
            return 0;
        }
        recov_time_reduction = h_MsCheckRange(recov_time_reduction, -100, 100);
        return recov_time_reduction;
    }


    //6401c0 - called 'once' after character takes turn -------------------------------------------------------- 
    public uint h_MsCommandComplete(uint chr_id, int param_2, int param_3) {

        //run the orignal function
        uint original_result = FFX2.FhCall.MsCommandComplete.chain_from(h_MsCommandComplete).fnptr!(chr_id, param_2, param_3);

        //Post-hook
        uint command_used = (uint)*(ushort*)(param_3 + 0xa4);
        //Time-trip handling - disable so you can't spam it over and Stop the enemy forever
        if (command_used == 0x31EA) {
            disable_time_Trip();
        }

        Chr* chr = h_MsGetChr(chr_id);
        int chr_base = (int)chr;
        //0xF3C to 0xF3D is the command that character last used, or a DS id on spherechange
        //if the character changed dressphere
        if (*(byte*)(chr_base + 0xF3D) == 0x50) {
            // overwrite ATB time remaining -- simulates a command with atb_cost of 20 - half as much wait as an item
            byte agility = *(byte*)(chr_base + 0x39a);
            int atb_length = (SPHERECHANGE_ATB_COST * 10000) / (agility + 1);
            if ( *(sbyte*)(chr_base + 0x4b8) > 0) { atb_length = atb_length / 2; }//haste also halves recovery time on spherchange
            if (*(sbyte*)(chr_base + 0x4b9) > 0) { atb_length = atb_length * 2; }//slow also doubles recovery time on spherchange

            *(int*)(chr_base + 0x9D8) = atb_length;
            // overwrite ATB timer length
            *(int*)(chr_base + 0x9DC) = atb_length;
        }

        // Status Handling
        TbStatusProcess(chr_id);
        TbRegenProcess();

        return original_result;
    }

    //function to disable Psychics Time Trip command --------------------------------------
    public void disable_time_Trip() {
        
            //get the commands data 
            int tt_exp_data = h_MsGetComData(0x31EA, (byte*)(0));
            int tt_mp_cost = h_MsGetComData(0x31EA, (byte*)(0));

            //set its com_dark_flag to true - this makes it drain HP instead of MP
            *(int*)(tt_exp_data + 0x14) |= (1 << 28);
            /*set its MP cost so high it requires all of the users HP -- command is greyed out because
             * it can't be used if HP is not high enough*/
            *(byte*)(tt_mp_cost + 0x26) = 128; 
        
    }

    
    // FH init ------------------------------------------------------------------------------------
    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        return FFX2.FhCall.MsATBgetRestTime.hook(this, h_MsATBgetRestTime)
        && FFX2.FhCall.MsCommandComplete.hook(this, h_MsCommandComplete)
        && FFX2.FhCall.MsGetChr.hook(this, h_MsGetChr)
        && _MsGetComData.hook(this, h_MsGetComData)
        && FhCall.MsCheckRange.hook(this, h_MsCheckRange)
        // additonal hooks
        && FFX2.FhCall.MsATBgetThinkingTime.hook(this, h_MsATBgetThinkingTime)
        //&& FFX2.FhCall.TOBtlDrawATBGaude.hook(this, h_TOBtlDrawATBGaude)
        // status handling hooks
        && FFX2.FhCall.MsStatusProcess.hook(this, h_MsStatusProcess)
        && FFX2.FhCall.MsStatCheckStop.hook(this, h_MsStatCheckStop)
        && FFX2.FhCall.MsATBActiveCheck.hook(this, h_MsATBActiveCheck)
        && FFX2.FhCall.MsCheckStatCount.hook(this, h_MsCheckStatCount)
        && FFX2.FhCall.FUN_00636690.hook(this, h_FUN_00636690)
        && FFX2.FhCall.MsStructClear.hook(this, h_MsStructClear)
        && FFX2.FhCall.MsDamageBufferExe.hook(this, h_MsDamageBufferExe)
        && FFX2.FhCall.MsSetStatus.hook(this, h_MsSetStatus)
        && FFX2.FhCall.MsSetChrWeak.hook(this, h_MsSetChrWeak)
        && FFX2.FhCall.MsStatusEffectCheck.hook(this, h_MsStatusEffectCheck)
        && FFX2.FhCall.MsMotionRecoverExe.hook(this, h_MsMotionRecoverExe);
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}

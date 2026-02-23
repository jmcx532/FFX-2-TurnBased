// SPDX-License-Identifier: MIT
/*A Fahrenheit module for FFX-2 that controls the Haste/Slow calculation and whether the ATB is slowed when hit
 * 
 * In X-2 a character in battle has 4? 'speed values' located at:
 * 
 * chr_base_addr + 0x9E4 (ATB countdown per tick)
 * chr_base_addr + 0x9E8 ()
 * chr_base_addr + 0x9EC (Animation Speed)
 * chr_base_addr + 0x9F0 ()
 * 
 */
namespace Fahrenheit.Modules.ChrAtbSpeedHandler;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//main speed function and parameters
public delegate uint speed_fx(uint chr_id, int chr_base_addr, uint speed_value);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//sub-function that checks for Stop/Petrify/Sleep
public delegate uint status_fx(uint chr_id, int param_2);

[FhLoad(FhGameId.FFX2)]
public class SpeedModule : FhModule {
    //offset address
    int addr_offset = 0x400000;

    private readonly FhMethodHandle<speed_fx> _speed_fx_handle;
    private readonly FhMethodHandle<status_fx> _status_fx_handle;

    //protected readonly FhLogger _logger;

    public SpeedModule() {
        //_logger = new FhLogger($"{FhUtil.get_timestamp_string()}_HasteMod.log");

        int speed_fx_addr = 0x634A20 - addr_offset;
        _speed_fx_handle = new FhMethodHandle<speed_fx>(this, "FFX-2.exe", speed_fx_addr, h_speed_fx);

        int status_fx_addr = 0x6430f0 - addr_offset;
        _status_fx_handle = new FhMethodHandle<status_fx>(this, "FFX-2.exe", status_fx_addr, h_status_fx);

    }

    public uint h_status_fx(uint chr_id, int param_2) {
        return _status_fx_handle.orig_fptr.Invoke(chr_id, param_2); 
    }


    /* param_1 is chr_id , 
     * param_2 is chr base addr, 
     * param_3 is DAT_00DF8818 (Base ATB Speed) (changes based on Config menu setting)
     * (Slow is 70, Normal is 95, and Fast is 120) - these can be changed in rom.bin
     */
    public unsafe uint h_speed_fx(uint chr_id, int chr_base_addr, uint speed_value) {

        //define an unified speed for the underlying ATB stuff and for status handling (not haste/slow - thats handled in ATB Recovery)
        //I only want to modify the speed value and variables for animation and other purposes
        
        uint uVar1;
        uint animation_speed;

        //Haste and Slow adjustment - left in to increase their animation speed properly
        //Haste calculation - checks character's Haste Timer and multiplies if it has time remaining
        if (*(byte*)(chr_base_addr + 0x43c) != '\0') {
            speed_value = (speed_value * 21) / 20;
        }
        //Slow calculation - checks character's Slow Timer and multiplies if it has time remaining
        if ((*(byte*)(chr_base_addr + 0x43d) != '\0') && speed_value > 1) {
            speed_value = speed_value / 2;
        }
        
        //Negative status adjustments
        //returns a bitfield where bit 0 is Stop status, bit 1 is petrify status and bit 2 is Sleep status
        uVar1 = _status_fx_handle.orig_fptr.Invoke(chr_id, 0);// FUN_006430f0(chr_id, 0);
        

        //if either Stop or Petrify bit is set, freeze the animation
        if ((uVar1 & 0x03) != 0) {
            animation_speed = 0;
        }
        else {
        //if neither Stopped or Petrified value is the same as atb_speed
            animation_speed = speed_value;
        }

        //if not under any of the three statuses, value is the ATB speed
        if (uVar1 == 0) {
            uVar1 = speed_value;
            
        }
        else {
        //if under any of the three statuses, value is 0
            uVar1 = 0;
        }


        //Write the values
        //speed value 2 - ?
        *(uint*)(chr_base_addr + 0x9f0) = speed_value;
        //*(uint*)(chr_base_addr + 0x9f0) = 0;
        //speed value 3 - writes the animation speed
        *(uint*)(chr_base_addr + 0x9ec) = animation_speed;
        //speed value 4 - This allows statuses to decrement
        *(uint*)(chr_base_addr + 0x9e8) = uVar1;
        //*(uint*)(chr_base_addr + 0x9e8) = 0;

        // If character is hit or ?, apply temporary ATB slow effect - removed for turn based
        /*
        if (((*(char*)(chr_base_addr + 0xd65) != '\0') ||
            (((*(byte*)(chr_base_addr + 0x3a6) & 2) == 0 && (*(char*)(chr_base_addr + 0xd98) != '\0')))) &&
           (1 < (int)uVar1)) {
            *(int*)(chr_base_addr + 0x9e4) = (int)uVar1 / 2;
            return (uint)((int)uVar1 / 2);
        }*/

        //speed value 1 - Write the character's ATB countdown per tick and return
        *(uint*)(chr_base_addr + 0x9e4) = uVar1;
        //*(uint*)(chr_base_addr + 0x9e4) = 0;
        return uVar1;
    }

    

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        return _speed_fx_handle.hook();
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}

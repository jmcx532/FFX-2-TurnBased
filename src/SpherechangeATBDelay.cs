// SPDX-License-Identifier: MIT

namespace Fahrenheit.Modules.SpherechangeATBDelay;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int get_chr_addr(uint chr_id);

//6401c0 - this writes ATB values after a command is used
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate uint atb_writer(byte arg_1, int arg_2/*not used*/, int arg_3);

[FhLoad(FhGameId.FFX2)]
public class SpherechangeATBDelay : FhModule {
    //protected readonly FhLogger _SClogger;

    private readonly FhMethodHandle<get_chr_addr> _get_chr_addr;
    private readonly FhMethodHandle<atb_writer> _atb_writer;

    public SpherechangeATBDelay() {

        int addr_offset = 0x400000;
        //_SClogger = new FhLogger($"{FhUtil.get_timestamp_string()}_SpherechangeATBDelay.log");

        _get_chr_addr = new FhMethodHandle<get_chr_addr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_get_chr_addr);
        _atb_writer = new FhMethodHandle<atb_writer>(this, "FFX-2.exe", 0x6401c0 - addr_offset, h_atb_writer);
    }

    /*this function gets the base address of a characters Battle data section
     * If it's parameter is less than 31 it returns a characters BattleData base address
     *Passed with 0 gets the first party members data, 1 the second and so on
     *If it's passed with a parameter greater than 155 it does end of battle cleanup I noticed from logging before
     */
    public int h_get_chr_addr(uint chr_id) {
        return _get_chr_addr.orig_fptr.Invoke(chr_id);
    }

    public unsafe uint h_atb_writer(byte chr_id, int arg_2, int arg_3) {
        //get the base character address
        int iVar2 = h_get_chr_addr(chr_id);

        //0xF3C to 0xF3D is the command that character last used, or a DS id on spherechange
        //if the character changed dressphere
        if (*(byte*)(iVar2 + 0xF3D) == 0x50) {
            //_SClogger.Info("Writing spherechange ATB");
            // overwrite ATB time remaining to a set (small) amount
            *(int*)(iVar2 + 0x9D8) = 0x0600;
            // overwrite ATB timer length
            *(int*)(iVar2 + 0x9DC) = 0x0600;

            return 0x0600;
        }//otherwise run as normal
        else { return _atb_writer.orig_fptr.Invoke(chr_id, arg_2, arg_3); }
    }

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _get_chr_addr.hook();
        _atb_writer.hook();
        return true;
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}

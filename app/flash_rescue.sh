# Capture the start time of this script
startTime=`date +%s`

# Make sure to clean up after ourselves
MYTMPDIR=`mktemp -d`
function Finish {
    exitCode=$?
    rm -rf $MYTMPDIR
    endTime=`date +%s`
    runtime=$((endTime-startTime))

    echo ""
    successMsg="FAILED"
    if [ $exitCode -eq 0 ]; then
        successMsg="completed SUCCESSFULLY"
    fi
    echo "Execution ${successMsg} in ${runtime} seconds"
}
trap Finish EXIT

# General function to safely execute a command that is passed in as one or more
# arguments, and return the STDOUT output to the caller
function ExecuteCmd {
    echo "************************************"
    echo "Executing $@"
    echo "************************************"
    "$@"
    local status=$?
    if [ $status -ne 0 ]; then
        echo "error running '$@' - exited with $1"
        exit 1
    fi
}

# FPGA
# -----------------------------------------------------------------------------
# Load the *.sof file            
ExecuteCmd nios2-configure-sof QMS_FPGA.sof

# Creating .flash file for the datafile
ExecuteCmd sof2flash --verbose --debug --epcq128 --input="QMS_FPGA.sof" --output="Fpga.flash"

# Programming flash with the FPGA configuration
ExecuteCmd nios2-flash-programmer --verbose --debug --epcs --base=0x10000 "Fpga.flash" --override nios2-flash-override.txt


# NIOS
# -----------------------------------------------------------------------------
# Creating .flash file for the datafile                                                
ExecuteCmd elf2flash --verbose --epcs --after="Fpga.flash" --input="app.elf" --output="Nios.flash"

## Programming flash with the datafile	
ExecuteCmd nios2-flash-programmer --verbose --debug --epcs --base=0x10000 "Nios.flash" --override nios2-flash-override.txt

# Remove the *.flash files from the directory
ExecuteCmd rm -f *.flash


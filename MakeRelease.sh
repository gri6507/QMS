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

# Capture the path to the directory containing this script. Change directories
# to here. This allows us to run with relative paths from here on.
SCRIPTPATH=$( cd $(dirname $0) ; pwd -P )
pushd "$SCRIPTPATH" >/dev/null

# Rebuild everything
ExecuteCmd cd bsp
${SCRIPTPATH}/colormake.sh clean
rm Makefile
ExecuteCmd cp settings_archived.bsp settings.bsp
ExecuteCmd nios2-bsp hal . ../qsys/*.sopcinfo --cpu-name NIOS_II_Processor
ExecuteCmd ${SCRIPTPATH}/colormake.sh all
ExecuteCmd cd ..
ExecuteCmd nios2-app-generate-makefile --debug --app-dir app --bsp-dir bsp --src-dir app --elf-name app.elf
ExecuteCmd cd app
ExecuteCmd ${SCRIPTPATH}/colormake.sh clean
export CFLAGS="-Werror -Wall"
ExecuteCmd ${SCRIPTPATH}/colormake.sh

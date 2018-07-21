set -e

# Make a folder for DAppChain instance
if [ ! -d ./build ]; then
    mkdir build
fi

cd build

# Download Loom binary if it is not present
if [ ! -f ./loom ]; then
    ../download-loom.sh 288
fi

if [ "$1" == "reset" ] && [ -d ./app.db ]; then
    ./loom reset
fi

cp ../genesis.json genesis.json

set +e

# Run loom init if it wasn't ran before
if [ ! -d ./app.db ]; then
    ./loom init
fi

set -e

./loom run
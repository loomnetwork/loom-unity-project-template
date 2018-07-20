pragma solidity ^0.4.24;

import "truffle/Assert.sol";
import "truffle/DeployedAddresses.sol";
import "../contracts/Blueprint.sol";

contract TestBlueprint {
    function testStoreLoad() public {
        Blueprint bluePrint = Blueprint(DeployedAddresses.Blueprint());

        bluePrint.store("key1", "value1");
        Assert.equal(bluePrint.load("key1"), "value1", "Value of 'key1' must be equal to 'value1'");
    }

    function testRemove() public {
        Blueprint bluePrint = Blueprint(DeployedAddresses.Blueprint());

        bluePrint.store("key1", "value1");
        bluePrint.remove("key1");
        Assert.equal(bluePrint.load("key1"), "", "Value of 'key1' must be empty");
    }
}
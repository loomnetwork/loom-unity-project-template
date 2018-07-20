pragma solidity ^0.4.24;

// A template contract that is just a string-to-string map.
contract Blueprint {
    event ValueChanged(string key, string newValue);
    event ValueRemoved(string key);

    mapping (string => string) database;

    constructor() public {
    }

    function store(string key, string value) public {
        database[key] = value;
        emit ValueChanged(key, value);
    }

    function load(string key) public view returns(string) {
        return database[key];
    }

    function remove(string key) public {
        delete database[key];
        emit ValueRemoved(key);
    }
}

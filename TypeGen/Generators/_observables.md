!!!Generating observable classes

**goal**: when developer already defined shape of api models, it should be great to help him with 
pre-generated models with observable properties and arrays

when api is declared using *interfaces*, it will generate
 1. proxy with *interfaces* used as models
 2. classes representing "implementation" of this interfaces. 
Inheritance of interfaces should be implemented as ("observable") interface implementation, class inheritance cannot be simply acquired from source.

when api is declared using *classes*, it will generate
 1. proxy with *interfaces* used as models
 2. classes representing "implementation" of this classes.
Class inheritance is copied into output, 
Inheritance of interfaces should be implemented as ("observable") interface implementation.

Because there is possibility that there exists interface called Ixxx and class called xxx (as its implementation), it is needed to do right picking of class/intf.


!!!Mapping
Implementation of observable class means, that there must me some JS-observable mapper, ko.mapping for example, or explicit (generated) methods (toJSON, fromJSON).
To consider: mapping to existing objects vs. creating new objects
Enums - names or int values

Naming convention - IObservableXXX for interfaces
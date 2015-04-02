# TypeGen
TypeScript generator (like CodeDom)

## Goal:
It consists of following parts (or layers)
 1. (optional) Generator / Transformation
  * generates TypeDom from (what you want)
  * ReflectionGenerator usees reflection to generate interfaces or classes
  * WebApiReflectionGenerator uses reflection to generate typed proxy for WebApi REST controllers 
  * SWAGGER generator to generate typed proxy for any REST api
 1. `TypeDom` - CodeDOM-like model of generated code
  * structure in semantic of TypeScript language - class, interface, member, function, etc.
  * it is only for declaration purposes, for implementation purposes (like function methods, lambdas, initial values, etc.) you can use "RawStatement" - i.e. string concatenation with possibility to reference another type
  * each model element contains ExtraData dictionary, which can contain any information about source generator
 1. OutputGenerator - generates source files of ts
  * it can generate modules, exports, ambiend declarations (.d.ts) files etc. 
  * it can be called from T4

User can 
* use or extend existing generators, generate TypeDom
	* i.e. use EnvDTE "live parsed" interfaces to process source files
	* i.e. change naming conventions
	* i.e. use fluent methods like `reflectionGenerator.AddTypesFromNamespace(typeof(MyClass)).Modularize(...)`
* modify or clone TypeDom
	* i.e. create interfaces for your WebApi in c#, create ViewModels for knockout with ko.observable and two-way converters (similary for angular)
* configure, override or extend generator to typescript

##Current state
* basic TypeDom
* basic OutputGenerator
* basic ReflectionGenerator


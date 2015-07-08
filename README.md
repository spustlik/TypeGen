# TypeGen
TypeScript generator (like CodeDom)

## Goal:
It consists of following parts (or layers)
 1. (optional) Generator / Transformation
  * generates TypeDom from any source, for example
  	* ReflectionGenerator uses reflection to generate interfaces or classes
  	* WebApiReflectionGenerator uses reflection to generate typed proxy for WebApi REST controllers 
  	* (in future) SWAGGER generator to generate typed proxy for any REST api
 1. `TypeDom` - CodeDOM-like model of generated code
  * structure in semantic model of TypeScript language
  	* class
  	* interface
  	* member (property, field)
  	* function
  	* etc.
  * model is for declaration and cross-referencing purposes
  * for implementation purposes (like function methods, lambdas, initial values, etc.)
  	* usage of "RawStatement" - i.e. string concatenation with possibility to reference another type, function, etc.
  * each model element contains ExtraData dictionary, which can contain any information about source generator
 1. OutputGenerator - generates TypeScript source files
  * it can generate modules, exports, ambient declarations (.d.ts) files etc. 
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
* TypeDom model with these elements
	* module,
	* class, interface, enum, 
	* function, member/property
	* generics (classes, function parameters)
	* raw statements, raw declarations
	* primitive types (any, array, number, string, boolean, void), extended types (Guid, DateTime, Array<T>)
* OutputGenerator
	* using TextFormatter to format output
	* using INameRessolver to ressolve any referenced declaration (class, interface, enum)
* ReflectionGenerator
	* generates TypeDom model from reflected types
	* using IReflectedNamingStrategy specifying how generated elements will be named
	* using IGenerationStrategy specifying which elements will be generated
	* all generated TypeDom elements have ExtraData reference to origin (i.e. Type, MethodInfo, etc.)
	* generator holds map from .NET type to Typescript type
* WebApiControllerGenerator
 	* generates TypeDom declarations from your WebApi controller(s)
 	* first it generates model of WebApi actions and it's parameters
 	* seconds it generates proxy definition using ReflectionGenerator to generate (or use) proxy data models

##ToDo
* SWAGGER (or another API description language) proxy generator implementation

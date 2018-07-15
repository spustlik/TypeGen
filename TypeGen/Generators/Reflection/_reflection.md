**Reflection generator from class to typescript interface**

recursivelly generates all members of class as properties.
- Inheritance of classes is generated via interface **implements** statement.
- classes from System namespace are not generated
- properties 
  - **[JsonIgnore]** attribute - these properties are ignored
  - **[JsonProperty]** attribute - if declared, PropertyName is used, NamingStrategy of Generator is used otherwise
- property type is determined by 
  - simple type: simple json/javascript type
  - enumerable: array
  - nullable: typescript nullable, if not reference type
  - generics: typescript generics
  - dictionary: typescript dictionary
  - enum: typescript enum is generated
  - JArray: any[], any other object from Newtonsoft. namespace
  - class/interface: recursivelly this interface is generated
  - others are taken as **any**
- this can be changed by deriving ReflectionGenerator and overriding some methods

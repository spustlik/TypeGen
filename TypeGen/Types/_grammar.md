## A.1 Types

  TypeParameters:  
   `<` TypeParameterList `>`

  TypeParameterList:  
   TypeParameter  
   TypeParameterList `,` TypeParameter

  TypeParameter:  
   Identifier Constraint(opt)

  Constraint:  
   `extends` Type

  TypeArguments:  
   `<` TypeArgumentList `>`

  TypeArgumentList:  
   TypeArgument  
   TypeArgumentList `,` TypeArgument

  TypeArgument:  
   Type

  Type:  
   PrimaryOrUnionType  
   FunctionType  
   ConstructorType

  PrimaryOrUnionType:  
   PrimaryType  
   UnionType

  PrimaryType:  
   ParenthesizedType  
   PredefinedType  
   TypeReference  
   ObjectType  
   ArrayType  
   TupleType  
   TypeQuery

  ParenthesizedType:  
   `(` Type `)`

  PredefinedType:  
   `any`  
   `number`  
   `boolean`  
   `string`  
   `void`

  TypeReference:  
   TypeName [no LineTerminator here] TypeArguments(opt)

  TypeName:  
   Identifier  
   ModuleName `.` Identifier

  ModuleName:  
   Identifier  
   ModuleName `.` Identifier

  ObjectType:  
   `{` TypeBody(opt) `}`

  TypeBody:  
   TypeMemberList `;`(opt)

  TypeMemberList:  
   TypeMember  
   TypeMemberList `;` TypeMember

  TypeMember:  
   PropertySignature  
   CallSignature  
   ConstructSignature  
   IndexSignature  
   MethodSignature

  ArrayType:  
   PrimaryType [no LineTerminator here] `[` `]`

  TupleType:  
   `[` TupleElementTypes `]`

  TupleElementTypes:  
   TupleElementType  
   TupleElementTypes `,` TupleElementType

  TupleElementType:  
   Type

  UnionType:  
   PrimaryOrUnionType `|` PrimaryType

  FunctionType:  
   TypeParameters(opt) `(` ParameterList(opt) `)` `=>` Type

  ConstructorType:  
   `new` TypeParameters(opt) `(` ParameterList(opt) `)` `=>` Type

  TypeQuery:  
   `typeof` TypeQueryExpression

  TypeQueryExpression:  
   Identifier  
   TypeQueryExpression `.` IdentifierName

  PropertySignature:  
   PropertyName `?`(opt) TypeAnnotation(opt)

  PropertyName:  
   IdentifierName  
   StringLiteral  
   NumericLiteral

  CallSignature:  
   TypeParameters(opt) `(` ParameterList(opt) `)` TypeAnnotation(opt)

  ParameterList:  
   RequiredParameterList  
   OptionalParameterList  
   RestParameter  
   RequiredParameterList `,` OptionalParameterList  
   RequiredParameterList `,` RestParameter  
   OptionalParameterList `,` RestParameter  
   RequiredParameterList `,` OptionalParameterList `,` RestParameter

  RequiredParameterList:  
   RequiredParameter  
   RequiredParameterList `,` RequiredParameter

  RequiredParameter:  
   AccessibilityModifier(opt) Identifier TypeAnnotation(opt)  
   Identifier `:` StringLiteral

  AccessibilityModifier:  
   `public`  
   `private`  
   `protected`

  OptionalParameterList:  
   OptionalParameter  
   OptionalParameterList `,` OptionalParameter

  OptionalParameter:  
   AccessibilityModifier(opt) Identifier `?` TypeAnnotation(opt)  
   AccessibilityModifier(opt) Identifier TypeAnnotation(opt) Initialiser  
   Identifier `?` `:` StringLiteral

  RestParameter:  
   `...` Identifier TypeAnnotation(opt)

  ConstructSignature:  
   `new` TypeParameters(opt) `(` ParameterList(opt) `)` TypeAnnotation(opt)

  IndexSignature:  
   `[` Identifier `:` `string` `]` TypeAnnotation  
   `[` Identifier `:` `number` `]` TypeAnnotation

  MethodSignature:  
   PropertyName `?`(opt) CallSignature

  TypeAliasDeclaration:  
   `type` Identifier `=` Type `;`

## A.2 Expressions

  PropertyAssignment:  ( Modified )  
   PropertyName `:` AssignmentExpression  
   PropertyName CallSignature `{` FunctionBody `}`  
   GetAccessor  
   SetAccessor

  GetAccessor:  
   `get` PropertyName `(` `)` TypeAnnotation(opt) `{` FunctionBody `}`

  SetAccessor:  
   `set` PropertyName `(` Identifier TypeAnnotation(opt) `)` `{` FunctionBody `}`

  CallExpression:  ( Modified )  
   …  
   `super` `(` ArgumentList(opt) `)`  
   `super` `.` IdentifierName

  FunctionExpression:  ( Modified )  
   `function` Identifier(opt) CallSignature `{` FunctionBody `}`

  AssignmentExpression:  ( Modified )  
   …  
   ArrowFunctionExpression

  ArrowFunctionExpression:  
   ArrowFormalParameters `=>` Block  
   ArrowFormalParameters `=>` AssignmentExpression

  ArrowFormalParameters:  
   CallSignature  
   Identifier

  Arguments:  ( Modified )  
   TypeArguments(opt) `(` ArgumentList(opt) `)`

  UnaryExpression:  ( Modified )  
   …  
   `<` Type `>` UnaryExpression

## A.3 Statements

  VariableDeclaration:  ( Modified )  
   Identifier TypeAnnotation(opt) Initialiser(opt)

  VariableDeclarationNoIn:  ( Modified )  
   Identifier TypeAnnotation(opt) InitialiserNoIn(opt)

  TypeAnnotation:  
   `:` Type

## A.4 Functions

  FunctionDeclaration:  ( Modified )  
   FunctionOverloads(opt) FunctionImplementation

  FunctionOverloads:  
   FunctionOverload  
   FunctionOverloads FunctionOverload

  FunctionOverload:  
   `function` Identifier CallSignature `;`

  FunctionImplementation:  
   `function` Identifier CallSignature `{` FunctionBody `}`

## A.5 Interfaces

  InterfaceDeclaration:  
   `interface` Identifier TypeParameters(opt) InterfaceExtendsClause(opt) ObjectType

  InterfaceExtendsClause:  
   `extends` ClassOrInterfaceTypeList

  ClassOrInterfaceTypeList:  
   ClassOrInterfaceType  
   ClassOrInterfaceTypeList `,` ClassOrInterfaceType

  ClassOrInterfaceType:  
   TypeReference

## A.6 Classes

  ClassDeclaration:  
   `class` Identifier TypeParameters(opt) ClassHeritage `{` ClassBody `}`

  ClassHeritage:  
   ClassExtendsClause(opt) ImplementsClause(opt)

  ClassExtendsClause:  
   `extends`  ClassType

  ClassType:  
   TypeReference

  ImplementsClause:  
   `implements` ClassOrInterfaceTypeList

  ClassBody:  
   ClassElements(opt)

  ClassElements:  
   ClassElement  
   ClassElements ClassElement

  ClassElement:  
   ConstructorDeclaration  
   PropertyMemberDeclaration  
   IndexMemberDeclaration

  ConstructorDeclaration:  
   ConstructorOverloads(opt) ConstructorImplementation

  ConstructorOverloads:  
   ConstructorOverload  
   ConstructorOverloads ConstructorOverload

  ConstructorOverload:  
   AccessibilityModifier(opt) `constructor` `(` ParameterList(opt) `)` `;`

  ConstructorImplementation:  
   AccessibilityModifier(opt) `constructor` `(` ParameterList(opt) `)` `{` FunctionBody `}`

  PropertyMemberDeclaration:  
   MemberVariableDeclaration  
   MemberFunctionDeclaration  
   MemberAccessorDeclaration

  MemberVariableDeclaration:  
   AccessibilityModifier(opt) `static`(opt) PropertyName TypeAnnotation(opt) Initialiser(opt) `;`

  MemberFunctionDeclaration:  
   MemberFunctionOverloads(opt) MemberFunctionImplementation

  MemberFunctionOverloads:  
   MemberFunctionOverload  
   MemberFunctionOverloads MemberFunctionOverload

  MemberFunctionOverload:  
   AccessibilityModifier(opt) `static`(opt) PropertyName CallSignature `;`

  MemberFunctionImplementation:  
   AccessibilityModifier(opt) `static`(opt) PropertyName CallSignature `{` FunctionBody `}`

  MemberAccessorDeclaration:  
   AccessibilityModifier(opt) `static`(opt) GetAccessor  
   AccessibilityModifier(opt) `static`(opt) SetAccessor

  IndexMemberDeclaration:  
   IndexSignature `;`

## A.7 Enums

  EnumDeclaration:  
   `enum` Identifier `{` EnumBody(opt) `}`

  EnumBody:  
   ConstantEnumMembers `,`(opt)  
   ConstantEnumMembers `,` EnumMemberSections `,`(opt)  
   EnumMemberSections `,`(opt)

  ConstantEnumMembers:  
   PropertyName  
   ConstantEnumMembers `,` PropertyName

  EnumMemberSections:  
   EnumMemberSection  
   EnumMemberSections `,` EnumMemberSection

  EnumMemberSection:  
   ConstantEnumMemberSection  
   ComputedEnumMember

  ConstantEnumMemberSection:  
   PropertyName `=` ConstantEnumValue  
   PropertyName `=` ConstantEnumValue `,` ConstantEnumMembers

  ConstantEnumValue:  
   SignedInteger  
   HexIntegerLiteral

  ComputedEnumMember:  
   PropertyName `=` AssignmentExpression

## A.8 Internal Modules

  ModuleDeclaration:  
   `module` IdentifierPath `{` ModuleBody `}`

  IdentifierPath:  
   Identifier  
   IdentifierPath `.` Identifier

  ModuleBody:  
   ModuleElements(opt)

  ModuleElements:  
   ModuleElement  
   ModuleElements ModuleElement

  ModuleElement:  
   Statement  
   `export`(opt) VariableDeclaration  
   `export`(opt) FunctionDeclaration  
   `export`(opt) ClassDeclaration  
   `export`(opt) InterfaceDeclaration  
   `export`(opt) TypeAliasDeclaration  
   `export`(opt) EnumDeclaration  
   `export`(opt) ModuleDeclaration  
   `export`(opt) ImportDeclaration  
   `export`(opt) AmbientDeclaration

  ImportDeclaration:  
   `import` Identifier `=` EntityName `;`

  EntityName:  
   ModuleName  
   ModuleName `.` Identifier

## A.9 Source Files and External Modules

  SourceFile:  
   ImplementationSourceFile  
   DeclarationSourceFile

  ImplementationSourceFile:  
   ImplementationElements(opt)

  ImplementationElements:  
   ImplementationElement  
   ImplementationElements ImplementationElement

  ImplementationElement:  
   ModuleElement  
   ExportAssignment  
   AmbientExternalModuleDeclaration  
   `export`(opt) ExternalImportDeclaration

  DeclarationSourceFile:  
   DeclarationElements(opt)

  DeclarationElements:  
   DeclarationElement  
   DeclarationElements DeclarationElement

  DeclarationElement:  
   ExportAssignment  
   AmbientExternalModuleDeclaration  
   `export`(opt) InterfaceDeclaration  
   `export`(opt) TypeAliasDeclaration  
   `export`(opt) ImportDeclaration  
   `export`(opt) AmbientDeclaration  
   `export`(opt) ExternalImportDeclaration

  ExternalImportDeclaration:  
   `import` Identifier `=` ExternalModuleReference `;`

  ExternalModuleReference:  
   `require` `(` StringLiteral `)`

  ExportAssignment:  
   `export` `=` Identifier `;`

## A.10 Ambients

  AmbientDeclaration:  
   `declare` AmbientVariableDeclaration  
   `declare` AmbientFunctionDeclaration  
   `declare` AmbientClassDeclaration  
   `declare` AmbientEnumDeclaration  
   `declare` AmbientModuleDeclaration

  AmbientVariableDeclaration:  
   `var` Identifier  TypeAnnotation(opt) `;`

  AmbientFunctionDeclaration:  
   `function` Identifier CallSignature `;`

  AmbientClassDeclaration:  
   `class` Identifier TypeParameters(opt) ClassHeritage `{` AmbientClassBody `}`

  AmbientClassBody:  
   AmbientClassBodyElements(opt)

  AmbientClassBodyElements:  
   AmbientClassBodyElement  
   AmbientClassBodyElements AmbientClassBodyElement

  AmbientClassBodyElement:  
   AmbientConstructorDeclaration  
   AmbientPropertyMemberDeclaration  
   IndexSignature

  AmbientConstructorDeclaration:  
   `constructor` `(` ParameterList(opt) `)` `;`

  AmbientPropertyMemberDeclaration:  
   AccessibilityModifier(opt) `static`(opt) PropertyName TypeAnnotation(opt) `;`  
   AccessibilityModifier(opt) `static`(opt) PropertyName CallSignature `;`

  AmbientEnumDeclaration:  
   `enum` Identifier `{` AmbientEnumBody(opt) `}`

  AmbientEnumBody:  
   AmbientEnumMemberList `,`(opt)

  AmbientEnumMemberList:  
   AmbientEnumMember  
   AmbientEnumMemberList `,` AmbientEnumMember

  AmbientEnumMember:  
   PropertyName  
   PropertyName = ConstantEnumValue

  AmbientModuleDeclaration:  
   `module` IdentifierPath `{` AmbientModuleBody `}`

  AmbientModuleBody:  
   AmbientModuleElements(opt)

  AmbientModuleElements:  
   AmbientModuleElement  
   AmbientModuleElements AmbientModuleElement

  AmbientModuleElement:  
   `export`(opt) AmbientVariableDeclaration  
   `export`(opt) AmbientFunctionDeclaration  
   `export`(opt) AmbientClassDeclaration  
   `export`(opt) InterfaceDeclaration  
   `export`(opt) AmbientEnumDeclaration  
   `export`(opt) AmbientModuleDeclaration  
   `export`(opt) ImportDeclaration

  AmbientExternalModuleDeclaration:  
   `module` StringLiteral `{`  AmbientExternalModuleBody `}`

  AmbientExternalModuleBody:  
   AmbientExternalModuleElements(opt)

  AmbientExternalModuleElements:  
   AmbientExternalModuleElement  
   AmbientExternalModuleElements AmbientExternalModuleElement

  AmbientExternalModuleElement:  
   AmbientModuleElement  
   ExportAssignment  
   `export`(opt) ExternalImportDeclaration


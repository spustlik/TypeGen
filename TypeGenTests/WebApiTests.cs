using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeGen;

namespace TypeGenTests
{
    [TestClass]
    public class WebApiTests
    {
        [TestMethod]
        public void TestModelGeneration()
        {
            /*
            var rg = new ReflectionGenerator();
            rg.GenerateTypes(ReflectionHelper.NamespaceTypes(typeof(WebApplication1.Models.CodeListModel)));
            var g = new OutputGenerator();
            g.GenerateReflection(rg);
            //toto nelze testovat, protoze se to bude menit
            Assert.IsTrue(g.Output.Length>0);
            
            var lastResult = @"
enum NodalSupportType {
    Fixed = 0,
    Hinged = 1,
    SlidingXY = 2,
    SlidingX = 3,
    SlidingY = 4
}
enum QueuedWorkState {
    Unknown = 0,
    Waiting = 1,
    Running = 2,
    Error = 3,
    Done = 4
}
enum UserStatus {
    Golden = 0,
    Silver = 1,
    Standard = 2
}
interface ICodeListModel {
    CodeGroups: ISectionCodeGroupModel[];
    Codes: ISectionCodeModel[];
    Shapes: ISectionShapeModel[];
    Types: ISectionSetTypeModel[];
    Sets: ISectionSetModel[];
}
interface IMaterialCategory {
    Id: number;
    Name: string;
    Color: string;
    ParentCategoryId: number;
    Materials: IMaterialModel[];
    SubCategories: IMaterialCategory[];
}
interface IMaterialModel {
    Id: number;
    Name: string;
    Color: string;
    NormName: string;
    CategoryId: number;
    CategoryName: string;
}
interface INosnikData {
    Length: number;
    SectionStartId: string;
    SectionEndId: string;
    MaterialId: string;
    SupportStart: NodalSupportType;
    SupportEnd?: NodalSupportType;
}
interface INosnikResult {
    Values: number[];
    Raw: string;
}
interface IQueuedWorkResult<T> {
    Id: string;
    State: QueuedWorkState;
    Result: T;
    Error: string;
    DurationSeconds: number;
}
interface ISectionCodeGroupModel {
    Id: number;
    Name: string;
    SectionCodes: number[];
}
interface ISectionCodeMapEntity {
    SectionCodeGroupId: number;
    SectionCodeId: number;
}
interface ISectionCodeModel {
    Id: number;
    Name: string;
    SectionCodeGroups: number[];
}
interface ISectionModel {
    Id: number;
    SectionSetId: number;
    Name: string;
    Version: number;
    ParameterValues: number[];
    XOutline: string;
}
interface ISectionParameterValueEntity {
    SectionId: number;
    ContainerOrder: number;
    Key: string;
    Value: number;
    ParameterDefinition: ISectionSetParameterModel;
}
interface ISectionSetModel {
    Id: number;
    Name: string;
    Version: number;
    SuperSymmetric: boolean;
    SectionSetTypeId: number;
    SectionCodeId: number;
    ShapeId: number;
    Shape: ISectionShapeModel;
    Note1: string;
    Note2: string;
    Source: string;
    Edition: string;
    XDimensions: string;
    XOutline: string;
    XElements: string;
    Sections: ISectionModel[];
    Parameters: ISectionSetParameterModel[];
}
interface ISectionSetParameterModel {
    Id: number;
    SectionSetId: number;
    Version: number;
    Key: string;
    Description: string;
    RichTextName: string;
    VisualGroup: number;
    OrderInGroup: number;
    Unit: string;
    UnitGroup: string;
    Formula: string;
}
interface ISectionSetTypeModel {
    Id: number;
    SetTypeId: number;
    Name: string;
    Version: number;
    Tooltip: string;
    Description: string;
    Sets: number[];
}
interface ISectionShapeModel {
    Id: number;
    ShapeId: number;
    ShapeIdEnum: number;
    Name: string;
}
interface IUserInfo {
    Name: string;
    Company: string;
    Status: UserStatus;
}
";*/
        }
    }
}

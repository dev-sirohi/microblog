const fs = require("fs");

function createProduct(index) {
    const base = `KV_2024_ITEM_${index}`;

    const colors = ["WHITE", "BLACK"];
    const sizes = [
        { Name: "Small", Code: "S" },
        { Name: "Medium", Code: "M" },
        { Name: "Large", Code: "L" }
    ];

    // Build variants from color × size
    const variants = [];
    let variantCount = 0;
    for (const color of colors) {
        for (const size of sizes) {
            variants.push({
                ItemNumber: `${base}-${color}-${size.Code}`,
                Description: `${base} ${base}-${color}-${size.Code}`,
                ItemBarcode: "",
                ItemUpc: "",
                ItemTypeName: "Inventory",
                StandardUnitPrice: 2.0,
                StandardUnitCost: 1.0,
                SellUomCode: "COIL",
                IsSellableFlag: true,
                IsTransferableFlag: true,
                IsPurchasableFlag: true,
                ReturnableFlag: true,
                ManufactureItemFlag: false,
                IsRawMaterialFlag: false,
                BrandName: "Belts",
                MaterialName: "100% Cotton",
                CustomDescription: "",
                Option1Name: "Color",
                Option1Value: color,
                Option2Name: "Shirt Size",
                Option2Value: size.Name,
                ActiveFlag: "true"
            });
            variantCount++;
        }
    }

    return {
        Id: index,
        BodyHtml: "",
        Handle: base,
        BasePartNumber: base,
        CategoryId: null,
        ProductCategoryName: "",
        Title: base,
        Options: [
            {
                Id: 1,
                Name: "Color",
                Position: 1,
                Values: [
                    { Id: 1, OptionId: 1, Name: "WHITE", Code: "WHITE", Seq: 1 },
                    { Id: 2, OptionId: 1, Name: "BLACK", Code: "BLACK", Seq: 2 }
                ],
                ValueArr: ["WHITE", "BLACK"]
            },
            {
                Id: 2,
                Name: "Shirt Size",
                Position: 2,
                Values: [
                    { Id: 3, OptionId: 2, Name: "Small", Code: "S", Seq: 1 },
                    { Id: 4, OptionId: 2, Name: "Medium", Code: "M", Seq: 2 },
                    { Id: 5, OptionId: 2, Name: "Large", Code: "L", Seq: 3 }
                ],
                ValueArr: ["Small", "Medium", "Large"]
            }
        ],
        ActiveFlag: true,
        CreateDttm: "2025-01-01T00:00:00",
        CreateSource: "TEST",
        ModifyDttm: null,
        ModifySource: null,
        Variants: variants,
        ProductImages: [],
        ProductTags: "",
        DefaultProductImagePath: "",
        ProductCustomizationProfile: "",
        IsHeaderOnly: false,
        MinimumSellQty: null,
        IsSelectiveImport: false,
        ColumnsToUpdate: "",
        ExcludeColumns: ""
    };
}

const COUNT = 10;
const products = [];

for (let i = 1; i <= COUNT; i++) {
    products.push(createProduct(i));
}

fs.writeFileSync("products.json", JSON.stringify(products, null, 2));
console.log("Generated products.json with", COUNT, "records");

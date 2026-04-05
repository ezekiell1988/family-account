using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedInventoryDemos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[,]
                {
                    { 1, null, "REV-COCA-001", null, 4, 1, "Coca-Cola 355ml", null, null, null },
                    { 2, null, "MP-CHILE-001", null, 1, 3, "Chile Seco", null, null, null },
                    { 4, null, "MP-SAL-001", null, 1, 3, "Sal", null, null, null },
                    { 5, null, "MP-FRASCO-001", null, 1, 1, "Frasco 250ml", null, null, null },
                    { 6, null, "PT-CHILE-EMB-001", null, 3, 1, "Chile Embotellado Marca X", null, null, null },
                    { 7, null, "MP-PAN-HD-001", null, 1, 1, "Pan de Hot Dog", null, null, null },
                    { 8, null, "MP-SALCHICHA-001", null, 1, 1, "Salchicha", null, null, null },
                    { 11, null, "PT-HOT-DOG-001", null, 3, 1, "Hot Dog", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "isVariantParent", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[] { 12, null, "CAMISA-OXF-000", null, 4, 1, true, "Camisa Oxford", null, null, null });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[,]
                {
                    { 18, null, "MP-HARINA-001", null, 1, 3, "Harina de Trigo", null, null, null },
                    { 23, null, "MP-MOZZ-001", null, 1, 3, "Queso Mozzarella", null, null, null },
                    { 24, null, "MP-PEPPERONI-001", null, 1, 3, "Pepperoni", null, null, null },
                    { 25, null, "MP-PINA-001", null, 1, 3, "Piña en Rodajas", null, null, null },
                    { 26, null, "MP-JAMON-001", null, 1, 3, "Jamón", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "hasOptions", "idProductParent", "idProductType", "idUnit", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[] { 27, null, "PT-PIZZA-001", true, null, 3, 1, "Pizza", null, null, null });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[,]
                {
                    { 28, null, "REV-SPRITE-001", null, 4, 1, "Sprite 355ml", null, null, null },
                    { 29, null, "REV-AGUA-BOT-001", null, 4, 1, "Agua Pura Botella 500ml", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "isCombo", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[] { 30, null, "COMBO-2PIZ-BEB", null, 4, 1, true, "Combo 2 Pizzas + Bebida", null, null, null });

            migrationBuilder.InsertData(
                table: "unitOfMeasure",
                columns: new[] { "idUnit", "codeUnit", "idUnitType", "nameUnit" },
                values: new object[,]
                {
                    { 5, "GR", 3, "Gramo" },
                    { 6, "ML", 2, "Mililitro" },
                    { 7, "LTR", 2, "Litro" }
                });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[,]
                {
                    { 3, null, "MP-VINAGRE-001", null, 1, 7, "Vinagre Blanco", null, null, null },
                    { 9, null, "MP-MOSTAZA-001", null, 1, 6, "Mostaza", null, null, null },
                    { 10, null, "MP-CATSUP-001", null, 1, 6, "Catsup", null, null, null },
                    { 13, null, "CAMISA-OXF-S-AZ", 12, 4, 1, "Camisa Oxford Talla S Azul", null, null, null },
                    { 14, null, "CAMISA-OXF-M-AZ", 12, 4, 1, "Camisa Oxford Talla M Azul", null, null, null },
                    { 15, null, "CAMISA-OXF-L-AZ", 12, 4, 1, "Camisa Oxford Talla L Azul", null, null, null },
                    { 16, null, "CAMISA-OXF-S-RJ", 12, 4, 1, "Camisa Oxford Talla S Rojo", null, null, null },
                    { 17, null, "CAMISA-OXF-M-RJ", 12, 4, 1, "Camisa Oxford Talla M Rojo", null, null, null },
                    { 19, null, "MP-AGUA-001", null, 1, 7, "Agua", null, null, null },
                    { 20, null, "MP-LEVADURA-001", null, 1, 5, "Levadura", null, null, null },
                    { 21, null, "MP-ACEITE-001", null, 1, 6, "Aceite de Oliva", null, null, null },
                    { 22, null, "MP-SALSA-TOM-001", null, 1, 6, "Salsa de Tomate", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "productAttribute",
                columns: new[] { "idProductAttribute", "idProduct", "nameAttribute", "sortOrder" },
                values: new object[,]
                {
                    { 1, 12, "Talla", 1 },
                    { 2, 12, "Color", 2 }
                });

            migrationBuilder.InsertData(
                table: "productComboSlot",
                columns: new[] { "idProductComboSlot", "idProductCombo", "isRequired", "nameSlot", "quantity", "sortOrder" },
                values: new object[,]
                {
                    { 1, 30, true, "Pizza #1", 1m, 1 },
                    { 2, 30, true, "Pizza #2", 1m, 2 },
                    { 3, 30, true, "Bebida", 1m, 3 }
                });

            migrationBuilder.InsertData(
                table: "productOptionGroup",
                columns: new[] { "idProductOptionGroup", "idProduct", "isRequired", "maxSelections", "minSelections", "nameGroup", "sortOrder" },
                values: new object[,]
                {
                    { 1, 27, true, 1, 1, "Elige tu tamaño", 1 },
                    { 2, 27, true, 1, 1, "Elige tu masa", 2 },
                    { 3, 27, true, 1, 1, "Elige tu sabor", 3 }
                });

            migrationBuilder.InsertData(
                table: "productOptionGroup",
                columns: new[] { "idProductOptionGroup", "idProduct", "maxSelections", "nameGroup", "sortOrder" },
                values: new object[] { 4, 27, 3, "Extras", 4 });

            migrationBuilder.InsertData(
                table: "productRecipe",
                columns: new[] { "idProductRecipe", "createdAt", "descriptionRecipe", "idProductOutput", "isActive", "nameRecipe", "quantityOutput", "versionNumber" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 6, true, "Receta Chile Embotellado", 1m, 1 },
                    { 2, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 11, true, "Receta Hot Dog", 1m, 1 },
                    { 3, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 27, true, "Base Pizza", 1m, 1 }
                });

            migrationBuilder.InsertData(
                table: "productRecipe",
                columns: new[] { "idProductRecipe", "createdAt", "descriptionRecipe", "idProductOutput", "nameRecipe", "quantityOutput", "versionNumber" },
                values: new object[,]
                {
                    { 4, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 27, "Opción Sabor: Pepperoni", 1m, 2 },
                    { 5, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 27, "Opción Sabor: Hawaiian", 1m, 3 },
                    { 6, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 27, "Opción Tamaño: Grande", 1m, 4 },
                    { 7, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 27, "Opción Extra: Doble Queso", 1m, 5 }
                });

            migrationBuilder.InsertData(
                table: "attributeValue",
                columns: new[] { "idAttributeValue", "idProductAttribute", "nameValue", "sortOrder" },
                values: new object[,]
                {
                    { 1, 1, "S", 1 },
                    { 2, 1, "M", 2 },
                    { 3, 1, "L", 3 },
                    { 4, 2, "Azul", 1 },
                    { 5, 2, "Rojo", 2 }
                });

            migrationBuilder.InsertData(
                table: "productComboSlotProduct",
                columns: new[] { "idProductComboSlotProduct", "idProduct", "idProductComboSlot", "sortOrder" },
                values: new object[,]
                {
                    { 1, 27, 1, 1 },
                    { 2, 27, 2, 1 },
                    { 3, 1, 3, 1 },
                    { 4, 28, 3, 2 },
                    { 5, 29, 3, 3 }
                });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "isDefault", "nameItem", "sortOrder" },
                values: new object[] { 1, 1, null, true, "Mediana", 1 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "nameItem", "priceDelta", "sortOrder" },
                values: new object[] { 2, 1, 6, "Grande", 2.00m, 2 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "isDefault", "nameItem", "sortOrder" },
                values: new object[] { 3, 2, null, true, "Clásica", 1 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "nameItem", "sortOrder" },
                values: new object[] { 4, 2, null, "Delgada", 2 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "isDefault", "nameItem", "sortOrder" },
                values: new object[] { 5, 3, 4, true, "Pepperoni", 1 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "nameItem", "sortOrder" },
                values: new object[] { 6, 3, 5, "Hawaiian", 2 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "nameItem", "priceDelta", "sortOrder" },
                values: new object[] { 7, 4, 7, "Doble Queso", 0.75m, 1 });

            migrationBuilder.InsertData(
                table: "productRecipeLine",
                columns: new[] { "idProductRecipeLine", "idProductInput", "idProductRecipe", "quantityInput", "sortOrder" },
                values: new object[,]
                {
                    { 1, 2, 1, 0.2000m, 1 },
                    { 2, 3, 1, 0.0500m, 2 },
                    { 3, 4, 1, 0.0050m, 3 },
                    { 4, 5, 1, 1.0000m, 4 },
                    { 5, 7, 2, 1.0000m, 1 },
                    { 6, 8, 2, 1.0000m, 2 },
                    { 7, 9, 2, 15.0000m, 3 },
                    { 8, 10, 2, 20.0000m, 4 },
                    { 9, 18, 3, 0.4000m, 1 },
                    { 10, 19, 3, 0.2500m, 2 },
                    { 11, 20, 3, 5.0000m, 3 },
                    { 12, 21, 3, 30.0000m, 4 },
                    { 13, 22, 3, 100.000m, 5 },
                    { 14, 23, 3, 0.1500m, 6 },
                    { 15, 24, 4, 0.1000m, 1 },
                    { 16, 25, 5, 0.0800m, 1 },
                    { 17, 26, 5, 0.0800m, 2 },
                    { 18, 18, 6, 0.1500m, 1 },
                    { 19, 19, 6, 0.0800m, 2 },
                    { 20, 23, 7, 0.0500m, 1 }
                });

            migrationBuilder.InsertData(
                table: "productComboSlotPresetOption",
                columns: new[] { "idProductComboSlotPresetOption", "idProductComboSlot", "idProductOptionItem" },
                values: new object[,]
                {
                    { 1, 1, 2 },
                    { 2, 2, 2 }
                });

            migrationBuilder.InsertData(
                table: "productVariantAttribute",
                columns: new[] { "idProductVariantAttribute", "idAttributeValue", "idProduct" },
                values: new object[,]
                {
                    { 1, 1, 13 },
                    { 2, 4, 13 },
                    { 3, 2, 14 },
                    { 4, 4, 14 },
                    { 5, 3, 15 },
                    { 6, 4, 15 },
                    { 7, 1, 16 },
                    { 8, 5, 16 },
                    { 9, 2, 17 },
                    { 10, 5, 17 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "productComboSlotPresetOption",
                keyColumn: "idProductComboSlotPresetOption",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "productComboSlotPresetOption",
                keyColumn: "idProductComboSlotPresetOption",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "productComboSlotProduct",
                keyColumn: "idProductComboSlotProduct",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "productComboSlotProduct",
                keyColumn: "idProductComboSlotProduct",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "productComboSlotProduct",
                keyColumn: "idProductComboSlotProduct",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "productComboSlotProduct",
                keyColumn: "idProductComboSlotProduct",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "productComboSlotProduct",
                keyColumn: "idProductComboSlotProduct",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "productOptionItem",
                keyColumn: "idProductOptionItem",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "productOptionItem",
                keyColumn: "idProductOptionItem",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "productOptionItem",
                keyColumn: "idProductOptionItem",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "productOptionItem",
                keyColumn: "idProductOptionItem",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "productOptionItem",
                keyColumn: "idProductOptionItem",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "productOptionItem",
                keyColumn: "idProductOptionItem",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "productRecipeLine",
                keyColumn: "idProductRecipeLine",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "productVariantAttribute",
                keyColumn: "idProductVariantAttribute",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "productVariantAttribute",
                keyColumn: "idProductVariantAttribute",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "productVariantAttribute",
                keyColumn: "idProductVariantAttribute",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "productVariantAttribute",
                keyColumn: "idProductVariantAttribute",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "productVariantAttribute",
                keyColumn: "idProductVariantAttribute",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "productVariantAttribute",
                keyColumn: "idProductVariantAttribute",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "productVariantAttribute",
                keyColumn: "idProductVariantAttribute",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "productVariantAttribute",
                keyColumn: "idProductVariantAttribute",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "productVariantAttribute",
                keyColumn: "idProductVariantAttribute",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "productVariantAttribute",
                keyColumn: "idProductVariantAttribute",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "attributeValue",
                keyColumn: "idAttributeValue",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "attributeValue",
                keyColumn: "idAttributeValue",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "attributeValue",
                keyColumn: "idAttributeValue",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "attributeValue",
                keyColumn: "idAttributeValue",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "attributeValue",
                keyColumn: "idAttributeValue",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "productComboSlot",
                keyColumn: "idProductComboSlot",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "productComboSlot",
                keyColumn: "idProductComboSlot",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "productComboSlot",
                keyColumn: "idProductComboSlot",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "productOptionGroup",
                keyColumn: "idProductOptionGroup",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "productOptionGroup",
                keyColumn: "idProductOptionGroup",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "productOptionGroup",
                keyColumn: "idProductOptionGroup",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "productOptionItem",
                keyColumn: "idProductOptionItem",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "productRecipe",
                keyColumn: "idProductRecipe",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "productRecipe",
                keyColumn: "idProductRecipe",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "productRecipe",
                keyColumn: "idProductRecipe",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "productRecipe",
                keyColumn: "idProductRecipe",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "productRecipe",
                keyColumn: "idProductRecipe",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "productRecipe",
                keyColumn: "idProductRecipe",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "productAttribute",
                keyColumn: "idProductAttribute",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "productAttribute",
                keyColumn: "idProductAttribute",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "productOptionGroup",
                keyColumn: "idProductOptionGroup",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "productRecipe",
                keyColumn: "idProductRecipe",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "idProduct",
                keyValue: 27);
        }
    }
}

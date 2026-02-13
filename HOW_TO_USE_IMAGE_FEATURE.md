# ?? How to Use Image Feature - User Guide

## ? Overview

Your inventory management system already has **full image support** for Part Types and Manufacturers. This guide shows you exactly how to use it!

---

## ?? Adding Images to Part Types

### Step-by-Step:

1. **Navigate to "Add New Item" page**
   - Click on "Add New Item" from the main menu

2. **Find the Part Type section** (gray box near the top)
   - You'll see:
     - A text box for entering the part type name
     - A **"Pick"** button (100px wide, black button)
     - An **"Add"** button (100px wide, black button)
     - Below: **"Image Path:"** label with a text box

3. **Add a Part Type with Image**:
   ```
   Step 1: Type part type name ? Example: "Compressor"
   Step 2: Click "Pick" button ? File browser opens
   Step 3: Select an image file ? .png, .jpg, .jpeg, .bmp, or .gif
   Step 4: See the path appear ? Example: "part-types\part-type-abc123.png"
   Step 5: Click "Add" button ? Saved to database!
   ```

4. **What you'll see**:
   - ? Message: "Part type added."
   - ? The path stays visible in the "Image Path:" text box until you add
   - ? After adding, the text boxes clear for next entry

---

## ?? Adding Logos to Manufacturers

### Step-by-Step:

1. **Navigate to "Add New Item" page**

2. **Find the Manufacturer section** (gray box below Part Type)
   - You'll see:
     - A text box for entering the manufacturer name
     - A **"Pick"** button (100px wide, black button)
     - An **"Add"** button (100px wide, black button)
     - Below: **"Logo Path:"** label with a text box

3. **Add a Manufacturer with Logo**:
   ```
   Step 1: Type manufacturer name ? Example: "Toyota"
   Step 2: Click "Pick" button ? File browser opens
   Step 3: Select a logo file ? .png, .jpg, .jpeg, .bmp, or .gif
   Step 4: See the path appear ? Example: "manufacturers\manufacturer-xyz789.png"
   Step 5: Click "Add" button ? Saved to database!
   ```

4. **What you'll see**:
   - ? Message: "Manufacturer added."
   - ? The path stays visible in the "Logo Path:" text box until you add
   - ? After adding, the text boxes clear for next entry

---

## ?? Viewing Images on Search Pages

### For Part Types:

1. **Navigate to "Search Items" page**
   
2. **See all Part Types displayed as cards**:
   ```
   Each card shows:
   ???????????????????????
   ?   [Image 64x64]     ?
   ?                     ?
   ?   Part Type Name    ?
   ?   Items: X          ?
   ?   Qty: Y            ?
   ?   [View Button]     ?
   ???????????????????????
   ```

3. **Images load automatically from database** ?

### For Manufacturers:

1. **Navigate to "Search Items" page**

2. **Click on any Part Type**

3. **See all Manufacturers displayed as cards**:
   ```
   Each card shows:
   ???????????????????????
   ?   [Logo 64x64]      ?
   ?                     ?
   ?  Manufacturer Name  ?
   ?   Items: X          ?
   ?   Qty: Y            ?
   ?   [View Button]     ?
   ???????????????????????
   ```

4. **Logos load automatically from database** ?

---

## ??? UI Layout - Add New Item Page

```
??????????????????????????????????????????????????????????????
?                    Add New Item                            ?
??????????????????????????????????????????????????????????????
?                                                            ?
?  Part Type                                                 ?
?  [ComboBox Dropdown ?]                                     ?
?                                                            ?
?  ???????????????????????????????????????????????????????? ?
?  ? Quick Add Part Type (Gray Box)                       ? ?
?  ?                                                      ? ?
?  ? [Name Textbox (50px high)  ] [Pick] [Add]          ? ?
?  ?                                                      ? ?
?  ? Image Path:                                          ? ?
?  ? part-types\part-type-abc123.png (shown after pick)  ? ?
?  ???????????????????????????????????????????????????????? ?
?                                                            ?
?  Manufacturer                                              ?
?  [ComboBox Dropdown ?]                                     ?
?                                                            ?
?  ???????????????????????????????????????????????????????? ?
?  ? Quick Add Manufacturer (Gray Box)                    ? ?
?  ?                                                      ? ?
?  ? [Name Textbox (50px high)  ] [Pick] [Add]          ? ?
?  ?                                                      ? ?
?  ? Logo Path:                                           ? ?
?  ? manufacturers\manufacturer-xyz789.png (after pick)   ? ?
?  ???????????????????????????????????????????????????????? ?
?                                                            ?
?  [... rest of form ...]                                    ?
??????????????????????????????????????????????????????????????
```

---

## ?? File Storage

After picking images, they are stored in:

```
Your Application Folder/
??? assets/
    ??? part-types/
    ?   ??? part-type-abc123.png
    ?   ??? part-type-def456.jpg
    ?   ??? part-type-ghi789.png
    ?
    ??? manufacturers/
        ??? manufacturer-xyz789.png
        ??? manufacturer-uvw012.jpg
        ??? manufacturer-rst345.png
```

**Note**: Files are automatically copied here when you pick them!

---

## ?? Database Storage

When you click "Add", **two things** are saved:

1. **File Path** (text) ? `image_path` or `logo_path` column
   - Example: "part-types\\part-type-abc123.png"

2. **Image Binary** (BLOB) ? `image` or `logo` column
   - The actual image bytes
   - Used for displaying images in search pages

**Both** are saved automatically - you don't need to do anything extra!

---

## ? Supported Image Formats

- ? PNG (.png)
- ? JPEG (.jpg, .jpeg)
- ? Bitmap (.bmp)
- ? GIF (.gif)

**Recommended**: Use PNG or JPEG for best results

---

## ?? Button & Textbox Sizes

All elements are sized for optimal visibility:

- **Name TextBoxes**: 50px height, 16px font
- **"Pick" Buttons**: 50px height, 100px width, 16px font
- **"Add" Buttons**: 50px height, 100px width, 16px font
- **Path TextBoxes**: 50px height, 16px font, **text wraps** if path is long

---

## ?? Troubleshooting

### "I don't see the image on the search page"

**Possible causes**:
1. Image wasn't saved - try adding the Part Type/Manufacturer again
2. Image file was deleted from assets folder - pick the image again
3. Image format not supported - use PNG or JPEG

**Solution**:
- Re-add the Part Type/Manufacturer with a new image
- Make sure to click "Add" after picking the image

### "The path textbox is empty after I click Pick"

**Possible causes**:
1. You cancelled the file dialog
2. File access was denied

**Solution**:
- Try clicking "Pick" again
- Make sure you select a file and click "Open" in the file dialog

### "I can't find the Pick button"

**Location**:
- It's the middle button between the name textbox and Add button
- It's in the gray boxes for Part Type and Manufacturer
- It's 100px wide with text "Pick"

---

## ?? Summary

? **Adding images is easy**:
   1. Type name
   2. Click "Pick"
   3. Select file
   4. Click "Add"

? **Viewing images is automatic**:
   - Just go to Search Items page
   - Images appear on all cards

? **Everything is already working**:
   - No setup needed
   - No configuration required
   - Ready to use right now!

---

## ?? Need Help?

The feature is fully implemented and working. If you have questions:
1. Check that the application is running
2. Navigate to "Add New Item" page
3. Look for the gray boxes for Part Type and Manufacturer
4. Follow the steps above

**Happy image management!** ???

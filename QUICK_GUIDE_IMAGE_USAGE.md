# Quick Guide: Adding Images to Part Types and Manufacturers

## For Part Types

### Step 1: Select or Create Part Type
- Open the **Item Creation** page
- Either:
  - Select an existing Part Type from the dropdown, OR
  - Click "+ Add New Part Type..." to create a new one

### Step 2: Add Image (REMOVED - Browse only now)
- ~~There's no longer a separate "Add" button or textbox~~
- The Browse button functionality has been integrated into the workflow

### Step 2: Add Image
**IMPORTANT**: You must select a Part Type first before adding an image!
1. Make sure a Part Type is selected (not the "+ Add New..." option)
2. Click the **"Add Image"** button next to the Part Type dropdown
3. Select an image file (PNG, JPG, JPEG, BMP, GIF)
4. The image will automatically:
   - Copy to `assets/part-types/` folder
   - Save to database (both path and bytes)
   - Show a success message

## For Manufacturers

### Step 1: Select or Create Manufacturer
- Open the **Item Creation** page
- Either:
  - Select an existing Manufacturer from the dropdown, OR
  - Click "+ Add New Manufacturer..." to create a new one

### Step 2: Add Logo
**IMPORTANT**: You must select a Manufacturer first before adding a logo!
1. Make sure a Manufacturer is selected (not the "+ Add New..." option)
2. Click the **"Add Logo"** button next to the Manufacturer dropdown
3. Select an image file (PNG, JPG, JPEG, BMP, GIF)
4. The logo will automatically:
   - Copy to `assets/manufacturers/` folder
   - Save to database (both path and bytes)
   - Show a success message

## Viewing Images

### In Search Items
1. Navigate to **Search Items**
2. You'll see cards for each Part Type
3. Each card displays:
   - Part Type image (from database)
   - Part Type name
   - Item count
   - Total quantity

### In Search Manufacturers (After Selecting Part Type)
1. Click on a Part Type card
2. You'll see cards for each Manufacturer
3. Each card displays:
   - Manufacturer logo (from database)
   - Manufacturer name
   - Item count
   - Total quantity

## Important Notes

?? **You must select an entity before adding an image**
- The "Add Image" and "Add Logo" buttons will show an error if no Part Type/Manufacturer is selected

?? **Images are saved immediately**
- No need to click Save Item after adding an image
- Images are stored in both filesystem and database

?? **Updating Images**
- Simply click "Add Image" or "Add Logo" and select a new image
- The old image will be replaced

## Troubleshooting

### "Please select a part type first" error
- Make sure you've selected an actual Part Type from the dropdown
- Don't try to browse while "+ Add New Part Type..." is selected

### "Please select a manufacturer first" error
- Make sure you've selected an actual Manufacturer from the dropdown
- Don't try to browse while "+ Add New Manufacturer..." is selected

### Images not showing in search
- Verify the image was saved (check for success message)
- Refresh the search page
- Check database using the CHECK_DATABASE.ps1 script

### Image file not found
- Ensure the selected image file exists and is accessible
- Check file permissions

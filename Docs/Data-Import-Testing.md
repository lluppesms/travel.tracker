# Data Import Feature Testing Guide

This guide explains how to test the new data import functionality that supports both JSON and CSV file formats.

## Feature Overview

The Travel Tracker application now supports importing travel location data from two formats:
1. **JSON Format** - The original format with full support for all location fields
2. **CSV Format** - A simplified format with header row and basic location data

## Prerequisites

- Travel Tracker application running locally or deployed to Azure
- SQL Server database configured and accessible
- Sample data files (available in `/wwwroot/samples/`)

## CSV Format Specification

The CSV file must have the following header row (exact format):
```
RowId,Location,Arrival,Departure,Comments,Address,Latitude,Longitude
```

### Field Descriptions

| Field | Required | Description | Example |
|-------|----------|-------------|---------|
| RowId | No | Optional row identifier | 1 |
| Location | Yes | Name of the location | "Yellowstone National Park" |
| Arrival | No | Arrival date (ISO format) | 2024-06-15 |
| Departure | No | Departure date (ISO format) | 2024-06-18 |
| Comments | No | Notes about the visit | "Amazing geysers!" |
| Address | Yes | Full address with state | "Yellowstone National Park, WY 82190" |
| Latitude | Yes | Latitude coordinate | 44.427963 |
| Longitude | Yes | Longitude coordinate | -110.588455 |

### CSV Example

```csv
RowId,Location,Arrival,Departure,Comments,Address,Latitude,Longitude
1,Yellowstone National Park,2024-06-15,2024-06-18,Amazing geysers,"Yellowstone National Park, WY 82190",44.427963,-110.588455
2,Grand Canyon National Park,2024-07-01,2024-07-03,Stunning views,"Grand Canyon, AZ 86023",36.1069,-112.1129
```

### Important Notes

- **State Extraction**: The system will automatically extract the 2-letter state code from the Address field
- **City Extraction**: The system attempts to extract city information from the Address field
- **Quoted Fields**: Fields containing commas should be enclosed in double quotes
- **Location Type**: CSV imports default to "Other" for location type (can be edited after import)
- **Rating**: CSV imports default to 0 for rating (can be edited after import)

## JSON Format Specification

The JSON format provides full control over all location fields:

```json
{
  "locations": [
    {
      "name": "Yellowstone National Park",
      "locationType": "National Park",
      "address": "Yellowstone National Park",
      "city": "Yellowstone",
      "state": "WY",
      "zipCode": "82190",
      "latitude": 44.427963,
      "longitude": -110.588455,
      "startDate": "2024-06-15",
      "endDate": "2024-06-18",
      "rating": 5,
      "comments": "Amazing geysers and wildlife!",
      "tags": ["national-park", "camping", "hiking"]
    }
  ]
}
```

### JSON Field Descriptions

All fields are optional except `name` and `state`:
- **name**: Location name (required)
- **locationType**: Type of location (RV Park, National Park, etc.)
- **address**: Street address
- **city**: City name
- **state**: 2-letter state code (required)
- **zipCode**: ZIP/Postal code
- **latitude**: Latitude coordinate (decimal)
- **longitude**: Longitude coordinate (decimal)
- **startDate**: Visit start date (ISO 8601 format)
- **endDate**: Visit end date (ISO 8601 format)
- **rating**: Rating from 1-5
- **comments**: Notes about the visit
- **tags**: Array of tag strings

## Testing Steps

### 1. Access the Upload Page

1. Navigate to the Travel Tracker application
2. Click on **Upload** in the navigation menu
3. You should see two tabs: "JSON Upload" and "CSV Upload"

### 2. Test JSON Upload

1. Click the **JSON Upload** tab
2. Click **Download Sample** to get a sample JSON file
3. Click the file input and select the sample file
4. Click **Validate** to check the file format
5. Review validation messages (should show number of valid locations)
6. Click **Upload** to import the data
7. Verify the upload summary shows successful imports
8. Navigate to **Locations** page to see imported data

### 3. Test CSV Upload

1. Click the **CSV Upload** tab
2. Click **Download Sample** to get a sample CSV file
3. Click the file input and select the sample file
4. Click **Validate** to check the file format
5. Review validation messages (should confirm CSV header and data rows)
6. Click **Upload** to import the data
7. Verify the upload summary shows successful imports
8. Navigate to **Locations** page to see imported data
9. Verify that state codes were correctly extracted from addresses

### 4. Test Error Handling

#### Invalid JSON Format
1. Create a JSON file with invalid structure (e.g., missing "locations" array)
2. Attempt to validate - should show error about invalid JSON structure
3. Upload should be disabled

#### Invalid CSV Header
1. Create a CSV file with wrong header (e.g., "Name,Location,Date")
2. Attempt to validate - should show error about invalid header format
3. Upload should be disabled

#### Missing Required Fields
1. Create a JSON file with locations missing required fields (name or state)
2. Validate - should show warning about invalid locations
3. Upload may succeed but skip invalid locations

#### Invalid Coordinates
1. Create a CSV file with non-numeric latitude/longitude values
2. Validate - should show validation errors
3. Upload should report failed records

### 5. Verify Data Quality

After importing data, verify:

1. **Location Details**
   - Names are correct
   - Coordinates are accurate
   - Dates are parsed correctly (for CSV imports)

2. **State Extraction** (CSV only)
   - State codes were correctly extracted from addresses
   - Check locations with different address formats

3. **Default Values** (CSV only)
   - Location type defaults to "Other"
   - Rating defaults to 0
   - Can be edited after import

4. **Statistics**
   - Navigate to **Statistics** page
   - Verify imported locations appear in state counts
   - Verify date ranges include imported data

5. **Map Display**
   - Navigate to **Map** page
   - Verify imported locations appear on the map
   - Check that coordinates are correctly displayed

## Performance Testing

### Small Files (1-10 locations)
- Should import almost instantly
- Progress bar may complete very quickly

### Medium Files (10-100 locations)
- Should import within a few seconds
- Progress bar should show incremental updates

### Large Files (100-1000 locations)
- May take 10-30 seconds depending on server performance
- Progress bar should provide feedback throughout
- Upload summary should show any failed records

### Maximum File Size
- File size limit is 10MB
- Files exceeding this will be rejected during validation
- Error message will indicate the size limit

## Troubleshooting

### "File size exceeds maximum"
- File is larger than 10MB
- Reduce the number of locations or split into multiple files

### "Invalid CSV header"
- Ensure the header row matches exactly: `RowId,Location,Arrival,Departure,Comments,Address,Latitude,Longitude`
- Check for extra spaces or typos

### "No valid locations found"
- Ensure at least one location has both name and state fields (JSON)
- Ensure at least one row has location name and coordinates (CSV)

### "State not extracted from address"
- Address field should contain a 2-letter state code (e.g., "CA", "NY", "TX")
- Format: "City Name, ST 12345" or "Address, City, ST"
- Edit the location after import to set the correct state

### Import Succeeds but Locations Don't Appear
- Check that you're logged in (if authentication is enabled)
- Verify you're viewing locations for the correct user
- Check the upload summary for the number of successful imports

### Duplicate Locations
- The system does not currently check for duplicates
- You may need to manually delete duplicate entries from the Locations page

## Sample Files

Pre-configured sample files are available:

1. **sample-locations.json** (located at `/wwwroot/samples/sample-locations.json`)
   - Contains 5 example locations with full field data
   - Includes national parks and RV parks
   - Shows proper JSON structure

2. **sample-locations.csv** (located at `/wwwroot/samples/sample-locations.csv`)
   - Contains 5 example locations matching CSV format
   - Shows proper header format
   - Demonstrates address format with state codes

## Security Considerations

- All file uploads are validated before processing
- Maximum file size is enforced at 10MB
- Only authenticated users can upload data (if authentication is configured)
- Uploaded data is associated with the current user's account
- SQL injection protection through parameterized queries
- Input validation on all fields

## API Information

For programmatic access, the following service methods are available:

```csharp
// Import from JSON stream
Task<ImportResult> ImportFromJsonAsync(Stream jsonStream, string userId)

// Import from CSV stream  
Task<ImportResult> ImportFromCsvAsync(Stream csvStream, string userId)

// Validate JSON without importing
Task<ValidationResult> ValidateJsonAsync(Stream jsonStream)

// Validate CSV without importing
Task<ValidationResult> ValidateCsvAsync(Stream csvStream)
```

## Known Limitations

1. **CSV Format**
   - Cannot specify location type (defaults to "Other")
   - Cannot specify rating (defaults to 0)
   - Cannot specify tags
   - City extraction may not work for all address formats

2. **Duplicate Detection**
   - No automatic duplicate detection
   - Users must manually manage duplicates

3. **Bulk Operations**
   - No bulk update capability (only bulk insert)
   - Cannot update existing locations via import

4. **Error Recovery**
   - Failed records are skipped but not stored for retry
   - Users must manually identify and re-import failed records

## Future Enhancements

Potential improvements for future versions:
- Bulk update capability
- Duplicate detection and merge options
- Support for additional file formats (Excel, KML, GPX)
- Asynchronous processing for very large files
- Import history and rollback capability
- Field mapping customization
- Progress tracking for large imports

## Support

For issues or questions about data import:
1. Check this documentation for common issues
2. Review validation error messages carefully
3. Verify file format against examples
4. Test with sample files first
5. Open an issue in the GitHub repository with details about the problem

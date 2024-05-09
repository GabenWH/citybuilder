#!/bin/bash

# Check if input file is provided
if [ "$#" -ne 2 ]; then
    echo "Usage: $0 input.osm output.geojson"
    exit 1
fi

input_file=$1
output_file=$2

# Check if the input file exists
if [ ! -f "$input_file" ]; then
    echo "Error: Input file does not exist."
    exit 1
fi

# Specify the layers you want to convert
declare -a layers=("points" "lines" "multilinestrings" "multipolygons" "other_relations")

# Create a temporary directory for the layer files
temp_dir=$(mktemp -d)
echo "Temporary directory created at $temp_dir"

# Convert each layer and store in the temporary directory
for layer in "${layers[@]}"; do
    echo "Converting $layer..."
    if ! ogr2ogr -f "GeoJSON" "$temp_dir/$layer.geojson" "$input_file" "$layer"; then
        echo "Conversion failed for $layer."
        exit 1
    fi
done

# Combine all GeoJSON files into a single output file
echo "Combining all layers into a single GeoJSON file..."
jq -s 'reduce .[] as $item ({}; .features += $item.features)' $temp_dir/*.geojson > $output_file

# Check if the combination was successful
if [ $? -eq 0 ]; then
    echo "Combined GeoJSON created successfully: ${output_file}"
else
    echo "Failed to combine GeoJSON files."
    exit 1
fi

# Clean up temporary files
rm -r $temp_dir
echo "Clean up completed."

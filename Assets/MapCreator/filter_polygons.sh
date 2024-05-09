#!/bin/bash

# Check if an argument was provided
if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <GeoJSON file>"
    exit 1
fi

input_file="$1"
output_file="${input_file%.geojson}-polygons.geojson"

jq '{
  type: .type,
  features: [ .features[] | select(.geometry.type == "Polygon" or .geometry.type == "MultiPolygon") ]
}' "$input_file" > "$output_file"

echo "Polygons have been filtered into: $output_file"


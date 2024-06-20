import json
from pyproj import Proj, Transformer
import os
import sys

# Retrieve file path from command line arguments
if len(sys.argv) < 3:
    print("Usage: python script.py <path_to_geojson_file> <path_to_centroid_geojson_file>")
    sys.exit(1)

input_geojson = sys.argv[1]
centroid_geojson = sys.argv[2]
output_geojson = os.path.splitext(input_geojson)[0] + '_utm' + os.path.splitext(input_geojson)[1]

# Function to read centroid from a GeoJSON file
def read_centroid(filepath):
    with open(filepath, 'r') as file:
        data = json.load(file)
        lon, lat = data['features'][0]['geometry']['coordinates']
        return lon, lat

centroid_lon, centroid_lat = read_centroid(centroid_geojson)

# Function to get UTM zone and create transformer
def get_transformer(lat, lon):
    utm_zone = int((lon + 180) / 6) + 1
    epsg_code = f'epsg:326{utm_zone:02d}' if lat >= 0 else f'epsg:327{utm_zone:02d}'
    proj_latlong = Proj('epsg:4326')
    proj_utm = Proj(epsg_code)
    return Transformer.from_proj(proj_latlong, proj_utm, always_xy=True), proj_utm

# Read the GeoJSON data
with open(input_geojson, 'r') as file:
    data = json.load(file)

transformer, proj_utm = get_transformer(centroid_lat, centroid_lon)
centroid_x, centroid_y = transformer.transform(centroid_lon, centroid_lat)

for feature in data['features']:
    if feature['geometry']['type'] == 'Polygon':
        for ring in feature['geometry']['coordinates']:
            for i in range(len(ring)):
                lon, lat = ring[i]
                x, y = transformer.transform(lon, lat)
                # Center the coordinates around the centroid
                ring[i] = [x - centroid_x, y - centroid_y]

# Write the updated GeoJSON
with open(output_geojson, 'w') as file:
    json.dump(data, file)

print("Conversion complete. Centered UTM coordinates have been written to:", output_geojson)

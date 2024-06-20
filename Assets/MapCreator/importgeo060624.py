import requests
import sys
import os
import geopandas as gpd
from osgeo import gdal, osr
import numpy as np
import json
from PIL import Image

api_key = ''
api_key_file = ''
centroid_file = ''

if len(sys.argv) <=3:
    sys.exit("Usage: python script.py <api_key_file> <centroid_file>")

api_key_file = sys.argv[1]
centroid_file = sys.argv[2]

side_length_km = 1.0  # 1 km square region

if len(sys.argv) <=4:
    side_length_km = float(sys.argv[3])
    print(f"modify side lenght to : {side_length_km}")
try:
    with open(api_key_file, 'r') as file:
        api_key = file.read().strip()
except FileNotFoundError:
    sys.exit(f"Error: The file {api_key_file} was not found.")
except Exception as e:
    sys.exit(f"An error occurred: {e}")

gdf = gpd.read_file(centroid_file)
print(f"side length: {side_length_km}")
# Now you can safely compute the centroid
centroid = gdf.geometry.centroid[0]
longitude, latitude = centroid.x, centroid.y

# Calculate distance per degree
latitude_degree_distance = 111.32  # in km, roughly the same worldwide
longitude_degree_distance = 111.32 * np.cos(np.radians(latitude))  # varies with latitude

# Desired side length in km (radius is half of this)

radius_latitude = side_length_km / 2.0 / latitude_degree_distance
radius_longitude = side_length_km / 2.0 / longitude_degree_distance

# Reprojection set up
utm_zone = int((centroid.x + 180) / 6) + 1
epsg_code = f"326{utm_zone}" if latitude > 0 else f"327{utm_zone}"

pre_UTM_file = f'terrain_data_{round(longitude, 2)}_{round(latitude, 2)}.tif'
post_UTM_file = f'terrain_data_UTM_{round(longitude, 2)}_{round(latitude, 2)}.tif'
post_UTM_file_PNG = f'terrain_data_UTM_{round(longitude, 2)}_{round(latitude, 2)}.png'

# Set your API key and URL
url = f"https://portal.opentopography.org/API/globaldem?demtype=SRTMGL3&west={longitude-radius_longitude}&south={latitude-radius_latitude}&east={longitude+radius_longitude}&north={latitude+radius_latitude}&outputFormat=GTiff&API_Key={api_key}"

# Make the request
response = requests.get(url)

# Save the file
if response.status_code == 200:
    with open(pre_UTM_file, 'wb') as file:
        file.write(response.content)
    print("Data downloaded and saved as " + pre_UTM_file)
else:
    sys.exit("Failed to download data: " + str(response.status_code))
# Open the dataset
dataset = gdal.Open(pre_UTM_file)
if not dataset:
    sys.exit("Error: Unable to open the dataset.")
# Define the UTM projection
utm_proj = osr.SpatialReference()
utm_proj.ImportFromEPSG(int(epsg_code))

# Calculate output bounds centered on the centroid
min_x = longitude - radius_longitude
max_x = longitude + radius_longitude
min_y = latitude - radius_latitude
max_y = latitude + radius_latitude

# Check if bounding box dimensions are valid
if min_x >= max_x or min_y >= max_y:
    sys.exit("Error: Invalid bounding box dimensions.")

# Reproject the dataset
warp_options = gdal.WarpOptions(dstSRS=f'EPSG:{epsg_code}', resampleAlg='bilinear',
                                outputBounds=(min_x, min_y, max_x, max_y))
result = gdal.Warp(post_UTM_file, dataset, options=warp_options)

if result:
    print("GeoTransform:", result.GetGeoTransform())
    print("Projection:", result.GetProjection())
    

    width = result.RasterXSize
    height = result.RasterYSize

    # Check if dataset dimensions are valid
    if width <= 0 or height <= 0:
        sys.exit("Error: Invalid dataset dimensions.")
    # Check if the projection includes UTM
    if 'UTM' in result.GetProjection():
        print("The dataset is in a UTM projection.")
    else:
        print("The dataset is not in a UTM projection.")
else:
    sys.exit("Error: Reprojection failed.")

# Get dimensions and pixel size
geotransform = result.GetGeoTransform()
pixel_width = geotransform[1]
pixel_height = abs(geotransform[5])  # Ensure positive value

# Get elevation data
band = result.GetRasterBand(1)
elevation_data = band.ReadAsArray()

# Calculate max elevation
max_elevation = int(np.max(elevation_data))
min_elevation = int(np.min(elevation_data))
dif_elevation = max_elevation - min_elevation

# Save results to JSON
results = {
    'height': dif_elevation,
    'pixel_width': pixel_width,
    'pixel_height': pixel_height,
    'width': width,
    'length': height,
    'HalfSideLength': side_length_km / 2.0,
    'file': post_UTM_file_PNG
}
file_path = os.path.join(os.getcwd(), f'terrain_data_{round(longitude, 2)}_{round(latitude, 2)}.json')
with open(file_path, 'w') as f:
    json.dump(results, f, indent=4)

# PNG format for Unity
def scale_pixel(value, min_val, max_val, scale_min, scale_max):
    # Scale a single pixel value from one range to another
    return ((value - min_val) / (max_val - min_val)) * (scale_max - scale_min) + scale_min

# Convert with Pillow
scaled_data = (elevation_data - min_elevation) / (max_elevation - min_elevation) * 65535
scaled_data = scaled_data.astype(np.uint16)

# Resize to the nearest power of two plus one
def next_power_of_two_plus_one(x):
    return 2**(x-1).bit_length() + 1

new_width = next_power_of_two_plus_one(width)
new_height = next_power_of_two_plus_one(height)
scaled_img = Image.fromarray(scaled_data, 'I;16').resize((new_width, new_height), Image.LANCZOS)

# Save as 16-bit PNG
scaled_img.save(post_UTM_file_PNG)

dataset = None
result = None

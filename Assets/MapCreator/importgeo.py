import requests
import sys
import os
import geopandas as gpd
from osgeo import gdal
import numpy as np
import json
from PIL import Image

api_key = ''
api_key_file = ''
centroid_file = ''

if len(sys.argv) < 1:
    sys.exit("Error: No arguments provided")

if len(sys.argv) != 3:
    sys.exit("Usage: python script.py <api_key_file> <centroid_file>")

if len(sys.argv) > 2:
    api_key_file = sys.argv[1]
    centroid_file = sys.argv[2]
try:
    with open(api_key_file,'r') as file:
        api_key = file.read()
except FileNotFoundError:
    sys.exit(f"Error: The file {api_key_file} was not found.")
except Exception as e:
    sys.exit(f"An error occurred: {e}")

gdf = gpd.read_file(centroid_file)

# Now you can safely compute the centroid
centroid = gdf.geometry.centroid[0]
longitude, latitude = centroid.x, centroid.y

# Reprojection set up
utm_zone = int((centroid.x + 180) / 6) + 1
epsg_code = f"326{utm_zone}" if latitude>0 else f"327{utm_zone}"

pre_UTM_file = f'terrain_data_{round(longitude,2)}_{round(latitude,2)}.tif'
post_UTM_file = f'terrain_data_UTM_{round(longitude,2)}_{round(latitude,2)}.tif'
post_UTM_file_PNG = f'terrain_data_UTM_{round(longitude,2)}_{round(latitude,2)}.png'


# Replace this eventually with something automatic
radius = 0.01








# Set your API key and URL
url = f"https://portal.opentopography.org/API/globaldem?demtype=SRTMGL3&west={longitude-radius}&south={latitude-radius}&east={longitude+radius}&north={latitude+radius}&outputFormat=GTiff&API_Key="+api_key

# Make the request
response = requests.get(url, headers={'Authorization': 'Bearer ' + api_key})

# Save the file
if response.status_code == 200:
    with open(pre_UTM_file, 'wb') as file:
        file.write(response.content)
    print("Data downloaded and saved as "+pre_UTM_file)
else:
    print("Failed to download data: ", response.status_code)

# Once we got the data let's analize it

dataset = gdal.Open(pre_UTM_file)
warp_options = gdal.WarpOptions(dstSRS=f'EPSG:{epsg_code}')

# Reproject the dataset
result = gdal.Warp(post_UTM_file, dataset, options=warp_options)

print("GeoTransform:", result.GetGeoTransform())
print("Projection:", result.GetProjection())

# Get the projection information
projection = result.GetProjection()
print("Projection WKT:", projection)

# Check if the projection includes UTM
if 'UTM' in projection:
    print("The dataset is in a UTM projection.")
else:
    print("The dataset is not in a UTM projection.")


# Get dimensions
width = result.RasterXSize
height = result.RasterYSize

# Get pixel size
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
results = {
    'height' : dif_elevation,
    'pixel_width' : pixel_width,
    'pixel_height' : pixel_height,
    'width' : width,
    'length' : height,
    'HalfSideLength': radius,
    'file':post_UTM_file_PNG
}
file_path = os.path.join(os.getcwd(),'terrain_data_'+str(round(longitude,2))+'_'+str(round(latitude,2))+'.json')
with open(file_path, 'w') as f:
    json.dump(results, f, indent=4)

# PNGFormat for Unity   

# Tiny little helper function
def scale_pixel(value, min_val, max_val, scale_min, scale_max):
    # Scale a single pixel value from one range to another
    return ((value - min_val) / (max_val - min_val)) * (scale_max - scale_min) + scale_min

# Convert with Pillow
# Note, GDAL produces a weird artifact when it produces pngs and I do not know why this is, this is why I didn't just save the data to png with GDAL. The artifact is nolonger present if you use Pillow
with Image.open(post_UTM_file) as img:
    img = img.convert('I')  # Convert to 32-bit integer pixels
    data = np.array(img)    # Convert to numpy array for easier manipulation

    vectorized_scale = np.vectorize(scale_pixel, otypes=[np.uint16])
    scaled_data = vectorized_scale(data, min_elevation, max_elevation, 0, 65535)

    # Convert scaled data back to an image
    scaled_img = Image.fromarray(scaled_data, 'I;16')  # 'I;16' creates a 16-bit image

    scaled_img.save(post_UTM_file_PNG)


dataset = None
result = None
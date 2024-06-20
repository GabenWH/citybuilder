import json
import itertools
import uuid
import pyproj
import sys
import os
import logging
import geojson
from shapely.geometry import LineString, mapping, MultiLineString, Point, shape
from shapely.ops import unary_union, linemerge, snap, transform
from pyproj import Proj, Transformer
from geojson import Point, Feature

current_script_path = os.path.dirname(os.path.abspath(__file__))

# Path for the log file
log_file_path = os.path.join(current_script_path, 'errorcords.log')

logging.basicConfig(filename=log_file_path, level=logging.ERROR,
                    format='%(asctime)s:%(levelname)s:%(message)s')

class LineWithProperties:
    def __init__(self, line, properties):
        self.line = line
        self.properties = properties

def load_geojson(file_path):
    with open(file_path) as f:
        return json.load(f)

def dump_geojson(data, file_path):
    with open(file_path, 'w') as f:
        json.dump(data, f, indent=4)

def find_intersections_and_segments_corrected(streets):
    centroid = unary_union([LineString(feature['geometry']['coordinates']) for feature in streets]).centroid
    merged = linemerge([LineString(feature['geometry']['coordinates']) for feature in streets])
    merged = snap(merged, merged, 0.00001)
    if isinstance(merged, LineString):
        merged = [merged]  # Make it a list so it's iterable
    elif isinstance(merged, MultiLineString):
        merged = [line for line in merged.geoms]

    intersections_dict = {}
    segments = []

    # Identifying intersections
    for line1, line2 in itertools.combinations(merged, 2):
        if line1.intersects(line2):
            intersection_point = line1.intersection(line2)
            if intersection_point.geom_type == 'MultiPoint':
                for pt in intersection_point.geoms:
                    intersections_dict[pt.wkt] = {'point': pt, 'segments': []}
            else:
                intersections_dict[intersection_point.wkt] = {'point': intersection_point, 'segments': []}
    
    # Creating segments and associating them with intersections
    for street in streets:
        line_and_properties = LineWithProperties(
            LineString(street['geometry']['coordinates']),
            street['properties']
        )
        segment_id = str(uuid.uuid4())
        for intersection_wkt in intersections_dict.keys():
            if line_and_properties.line.intersects(intersections_dict[intersection_wkt]['point']):
                intersections_dict[intersection_wkt]['segments'].append(segment_id)
        
        # Assuming line_and_properties.properties is a dictionary and already exists
        line_and_properties.properties['id'] = segment_id  # Add segment_id to the properties dictionary

        # Now append the dictionary to segments with the updated properties
        segments.append({
            'geometry': {mapping(line_and_properties.line)},
            'properties': line_and_properties.properties
    })

    intersections = [
        {
            'geometry': mapping(intersection['point']),
            'properties': {
                'connected_segments': intersection['segments']
            }
        }
        for intersection in intersections_dict.values()
    ]

    return intersections, segments, centroid
 
def center_and_scale_to_meters(segments, intersections, centroid):
    # Determine appropriate UTM zone for the centroid's longitude
    zone = int((centroid.x + 180) / 6) + 1
    projection = Proj(proj='utm', zone=zone, ellps='WGS84', datum='WGS84')
    transformer = Transformer.from_proj(Proj('epsg:4326'), projection, always_xy=True)

    # Transform centroid to projected system
    proj_centroid_x, proj_centroid_y = transformer.transform(centroid.x, centroid.y)

    # Helper function to apply transformation and recenter
    def transform_and_recenter(geometry):
        # Converts geometry from long/lat to 'epsg:4326' Projection
        transformed = transform(lambda x, y: transformer.transform(x,y),geometry)

        # Recenter the geometry
        recentered_point = transform(lambda x, y: (x - proj_centroid_x, y - proj_centroid_y), transformed)

        # Extracts Coordinates from the geojson data
        geojson_dict = mapping(recentered_point)

        # Check and handle missing 'coordinates'
        if 'coordinates' not in geojson_dict:
            # Save the problematic data to a file
            logging.error(geometry)
            logging.error(geojson_dict)
            logging.error(recentered_point)
            raise ValueError(f"Failed to find 'coordinates' in the transformed geometry, logged to: {log_file_path}")

        return geojson_dict['coordinates']

    meter_segments = []
    for segment in segments:
        try:
            # Preserve all segment data, only update the geometry
            new_segment = segment.copy()  # Assumes segment is a dictionary containing a 'geometry' key
            new_segment['geometry'] = transform_and_recenter(shape(segment['geometry']))
            meter_segments.append(new_segment)
        except ValueError as e:
            print(f"Error processing segment: {e}")

    # Transform intersections
    meter_intersections = []
    for intersection in intersections:
        try:
            # Preserves intersection data (!!!You will need all segment ids in unity!!!)
            new_intersection = intersection.copy()  # Assumes intersection is a dictionary containing a 'geometry' key
            new_intersection['geometry'] = transform_and_recenter(shape(intersection['geometry']))
            meter_intersections.append(new_intersection)
        except ValueError as e:
            print(f"Error processing intersection: {e}")
            print(f"Problomatic intersection:{intersection}")
    return meter_segments, meter_intersections

# Main function to process the GeoJSON file and output the segments and intersections JSON files
def process_geojson(geojson_path):
    streets_geojson = load_geojson(geojson_path)
    intersections, segments, centroid = find_intersections_and_segments_corrected(streets_geojson['features'])
    centroid_dict = {
        "type": "Feature",
        "geometry": mapping(centroid),
        "properties": {}
    }
    meter_segments, meter_intersections = center_and_scale_to_meters(segments,intersections,centroid)

    centroid_file_path = geojson_path.replace('.geojson', '-centroid.geojson')
    segments_file_path = geojson_path.replace('.geojson', '-segments.json')
    intersections_file_path = geojson_path.replace('.geojson', '-intersections.json')

    dump_geojson({'type': 'FeatureCollection', 'features': [centroid_dict]}, centroid_file_path)
    dump_geojson({'type': 'FeatureCollection', 'features': meter_segments}, segments_file_path)
    dump_geojson({'type': 'FeatureCollection', 'features': meter_intersections}, intersections_file_path)

    return segments_file_path, intersections_file_path, centroid_file_path

# Example usage

#geojson_path = 'path_to_your_geojson_file_here.geojson'
#segments_file_path, intersections_file_path = process_geojson(geojson_path)
#print(f"Segments file saved to: {segments_file_path}")
#print(f"Intersections file saved to: {intersections_file_path}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python script.py <path_to_geojson_file>")
    else:
        segments_file_path, intersections_file_path, centroid_file_path = process_geojson(sys.argv[1])
        
        print(f"Segments file saved to: {segments_file_path}")
        print(f"Intersections file saved to: {intersections_file_path}")
        print(f"Centroid saved to: {centroid_file_path}")
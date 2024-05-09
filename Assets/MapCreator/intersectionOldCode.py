from shapely.geometry import LineString, Point, MultiPoint
from shapely.ops import split
import json
import sys

def segment_streets_and_output_intersections(geojson_filepath, segments_output_filepath, intersections_output_filepath):
    with open(geojson_filepath, 'r') as file:
        geojson_data = json.load(file)

    lines = [LineString(feature["geometry"]["coordinates"]) for feature in geojson_data["features"]]
    all_intersections = []

    # Collecting all intersection points
    for i, line in enumerate(lines):
        for other_line in lines[i+1:]:
            if line.intersects(other_line):
                intersection_point = line.intersection(other_line)
                if isinstance(intersection_point, Point):
                    all_intersections.append(intersection_point)

    # Deduplicate intersection points
    unique_intersections = MultiPoint(all_intersections)

    street_segments = []
    intersections = []
    segment_id = 0

    # Process each line to segment streets and identify intersections
    for i, line in enumerate(lines):
        split_points = [point for point in unique_intersections if point.intersects(line)]
        if split_points:
            segments = split(line, MultiPoint(split_points))
            for segment in segments:
                if isinstance(segment, LineString):
                    street_segments.append({
                        "id": f"segment_{segment_id}",
                        "coordinates": list(segment.coords)
                    })
                    segment_id += 1
        else:
            street_segments.append({
                "id": f"segment_{segment_id}",
                "coordinates": list(line.coords)
            })
            segment_id += 1

    # Generate intersection data
    for i, intersection in enumerate(unique_intersections):
        intersections.append({
            "id": f"intersection_{i}",
            "point": [intersection.x, intersection.y]
        })

    # Output street segments to JSON
    with open(segments_output_filepath, 'w') as file:
        json.dump({"street_segments": street_segments}, file, indent=4)

    # Output intersections to a separate JSON
    with open(intersections_output_filepath, 'w') as file:
        json.dump({"intersections": intersections}, file, indent=4)

    print(f"Street segments output saved to {segments_output_filepath}")
    print(f"Intersections output saved to {intersections_output_filepath}")

if __name__ == "__main__":
    if len(sys.argv) < 4:
        print("Usage: python script.py <path_to_geojson_file> <segments_output_json_file> <intersections_output_json_file>")
    else:
        geojson_filepath = sys.argv[1]
        segments_output_filepath = sys.argv[2]
        intersections_output_filepath = sys.argv[3]
        segment_streets_and_output_intersections(geojson_filepath, segments_output_filepath, intersections_output_filepath)


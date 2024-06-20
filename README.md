City Builder

Welcome to the City Builder project! This Unity-based project aims to create a realistic and functional city simulation. It integrates Python scripts for downloading street and building layouts, working roads, intersection markings, and functional vehicles. Below is an overview of the features, installation instructions, and future goals.
Features

    Python Integration: The project uses Python scripts to download and process real-world data for street and building layouts.
    Working Roads: Roads are dynamically generated and can handle vehicle traffic.
    Intersection Markings: Proper markings at intersections to guide vehicles.
    Functional Vehicles: Vehicles can navigate the roads and intersections realistically.

Installation

    Clone the Repository:

    bash

git clone [your-repository-url]

Install Python Dependencies:
Navigate to the scripts directory and install the required Python packages.

bash

cd scripts
pip install -r requirements.txt

Run Python Scripts:
Download street and building layout data. The primary data source is OpenStreetMap.

bash

    # Download geographic data
    python3 importgeo.py <api_key_opentopography> <centroid>

    # Filter street data
    ./filterstreets.sh

    # Generate the necessary centroid
    python3 linesegmenter.py

    Import into Unity:
    Open the Unity project and ensure the downloaded data is correctly imported.

Getting Started

    Open the Unity project in your Unity Editor.
    Follow the setup instructions to integrate the downloaded street and building layouts.
    Test the existing functionality, including roads, intersections, and vehicle movement.

Future Goals

    Pedestrian Traffic: Simulate pedestrian movement throughout the city.
    Traffic Simulation: Full simulation of both vehicle and pedestrian traffic.
    Transit Simulation: Simulate the transit of goods and people within the city.

Contributing

We welcome contributions! Please read our Contributing Guidelines for more details.
License

This project is licensed under the GPL-3.0 License - see the LICENSE file for details.

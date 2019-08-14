#!/bin/bash

# Stop on any error
set -e

## Settings 
declare -a CLASS_CONFIGS=("ClassroomConfig-ADHD-None" "ClassroomConfig-ADHD-Medium" "ClassroomConfig-ADHD-High" "ClassroomConfig-ADHD-VeryHigh" "ClassroomConfig-ADHD-Ambitious")
#declare -a CLASS_CONFIGS=("ClassroomConfig-ADHD-None" "ClassroomConfig-ADHD-Medium")
OUTPUT_FOLDERS=()

CLASS_CONFIG_FOLDER="../Ressources/ClassroomConfigs"
SIMULATION_CONFIG="../Ressources/SimulationConfigs/SimulationConfigFile.json"
OUTFOLDER="."
NINSTANCES=5
SEED=424242

## Generate pictures
for CC in "${CLASS_CONFIGS[@]}"
do
    echo "Running simulation model ${CC} ..."
    python experiment.py ${SIMULATION_CONFIG} ${CLASS_CONFIG_FOLDER}/${CC}.json ${SEED} ${NINSTANCES} ${OUTFOLDER}/${CC}

    OUTPUT_FOLDERS+=(${OUTFOLDER}/${CC})
done

echo "Generating study results for " ${OUTPUT_FOLDERS[@]}

python study.py ${OUTPUT_FOLDERS[@]}
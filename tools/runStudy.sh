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
    python experiment.py ${CLASS_CONFIG_FOLDER}/${CC}.json ${OUTFOLDER}/${CC} --simulation_config_file ${SIMULATION_CONFIG} --seed ${SEED} --nInstances ${NINSTANCES} --headless --skip_agent_plots

    OUTPUT_FOLDERS+=(${OUTFOLDER}/${CC})
done

echo "Generating study results for " ${OUTPUT_FOLDERS[@]}

python study.py ${OUTPUT_FOLDERS[@]}